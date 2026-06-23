import { DatePipe } from '@angular/common';
import { Component, ElementRef, Pipe, PipeTransform, QueryList, ViewChild, ViewChildren, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

const apiBaseUrl = 'http://localhost:5293/api';

interface ChatRequest {
  conversationId?: string;
  message: string;
}

interface ChatResponse {
  conversationId: string;
  responseType: number;
  message: string;
  requiresApproval: boolean;
  toolName?: string | null;
  approvalId?: string | null;
  toolRequest?: ToolRequest | null;
  toolExecutionResult?: ToolExecutionResult | null;
  createdAtUtc: string;
}

interface ToolRequest {
  toolName: string;
  requiresApproval: boolean;
  reason: string;
  arguments: Record<string, string>;
}

interface ToolExecutionResult {
  toolName: string;
  succeeded: boolean;
  simulated: boolean;
  message: string;
  executedAtUtc: string;
}

interface ApprovalRecord {
  id: string;
  conversationId: string;
  toolRequest: ToolRequest;
  status: number;
  createdAtUtc: string;
  decidedAtUtc?: string | null;
  executedAtUtc?: string | null;
  executionResult?: ToolExecutionResult | null;
}

interface ShiroTask {
  id: string;
  title: string;
  isCompleted: boolean;
  createdAtUtc: string;
}

interface AuditLogEntry {
  id: string;
  eventType: number;
  approvalId?: string | null;
  conversationId: string;
  toolName: string;
  message: string;
  createdAtUtc: string;
}

interface ChatMessage {
  id: string;
  sender: 'user' | 'shiro';
  text: string;
  createdAtUtc: string;
  response?: ChatResponse;
}

interface ConversationSummary {
  id: string;
  title: string;
  messageCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

interface ConversationMessage {
  id: string;
  role: string;
  content: string;
  createdAtUtc: string;
}

@Pipe({ name: 'auditEventLabel' })
export class AuditEventLabelPipe implements PipeTransform {
  transform(value: number): string {
    switch (value) {
      case 1:
        return 'Approval requested';
      case 2:
        return 'Approval accepted';
      case 3:
        return 'Approval rejected';
      case 4:
        return 'Tool execution succeeded';
      case 5:
        return 'Tool execution failed';
      case 6:
        return 'Safe tool executed';
      default:
        return 'Audit event';
    }
  }
}

@Component({
  selector: 'app-root',
  imports: [DatePipe, FormsModule, AuditEventLabelPipe],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly http = inject(HttpClient);
  @ViewChild('messageInput') private messageInput?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('conversationPanel') private conversationPanel?: ElementRef<HTMLDivElement>;
  @ViewChildren('chatMessage') private chatMessageElements?: QueryList<ElementRef<HTMLElement>>;

  protected readonly messageText = signal('');
  protected readonly conversationId = signal<string | undefined>(undefined);
  protected readonly chatMessages = signal<ChatMessage[]>([]);
  protected readonly lastResponse = signal<ChatResponse | null>(null);
  protected readonly pendingApprovals = signal<ApprovalRecord[]>([]);
  protected readonly conversations = signal<ConversationSummary[]>([]);
  protected readonly tasks = signal<ShiroTask[]>([]);
  protected readonly auditEntries = signal<AuditLogEntry[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly failedChatMessage = signal<string | null>(null);
  protected readonly isSending = signal(false);
  protected readonly isWaitingForFirstToken = signal(false);
  protected readonly isRefreshing = signal(false);
  protected readonly copiedMessageId = signal<string | null>(null);
  private lastFailedUserMessage: string | null = null;

  constructor() {
    this.refreshAll();
  }

  protected usePrompt(prompt: string): void {
    this.messageText.set(prompt);
    this.focusMessageInput();
  }

  protected startNewConversation(): void {
    this.conversationId.set(undefined);
    this.chatMessages.set([]);
    this.lastResponse.set(null);
    this.failedChatMessage.set(null);
    this.errorMessage.set(null);
    this.messageText.set('');
    this.lastFailedUserMessage = null;
    this.focusMessageInput();
  }

  protected loadConversation(conversation: ConversationSummary): void {
    this.errorMessage.set(null);

    this.http.get<ConversationMessage[]>(`${apiBaseUrl}/conversations/${conversation.id}/messages`).subscribe({
      next: messages => {
        this.conversationId.set(conversation.id);
        this.chatMessages.set(messages.map(message => ({
          id: message.id,
          sender: message.role === 'user' ? 'user' : 'shiro',
          text: message.content,
          createdAtUtc: message.createdAtUtc
        })));
        this.focusMessageInput();

        const lastMessage = messages.at(-1);

        if (lastMessage) {
          this.scrollMessageToTop(lastMessage.id);
        }
      },
      error: error => {
        this.errorMessage.set(this.buildApiErrorMessage('load conversation history', error));
      }
    });
  }

  protected handleMessageKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Enter' || event.shiftKey) {
      return;
    }

    event.preventDefault();
    this.sendMessage();
  }

  protected async sendMessage(): Promise<void> {
    const message = this.messageText().trim();

    if (!message || this.isSending()) {
      return;
    }

    await this.sendChatMessage(message, true);
  }

  protected async retryLastMessage(): Promise<void> {
    if (!this.lastFailedUserMessage || this.isSending()) {
      return;
    }

    await this.sendChatMessage(this.lastFailedUserMessage, false);
  }

  protected async copyMessage(message: ChatMessage): Promise<void> {
    if (!message.text) {
      return;
    }

    await navigator.clipboard.writeText(message.text);
    this.copiedMessageId.set(message.id);
    setTimeout(() => {
      if (this.copiedMessageId() === message.id) {
        this.copiedMessageId.set(null);
      }
    }, 1600);
  }

  protected getSourceLabel(message: ChatMessage): string | null {
    if (message.sender !== 'shiro' || !message.response) {
      return null;
    }

    switch (message.response.toolName) {
      case 'weather_lookup':
        return 'Source: Open-Meteo weather service';
      case 'current_datetime':
        return 'Source: Local device clock';
      case 'create_task':
        return 'Source: Shiro local task tool';
      default:
        return 'Source: Ollama local model';
    }
  }

  protected getSourceUrl(message: ChatMessage): string | null {
    if (message.response?.toolName === 'weather_lookup') {
      return 'https://open-meteo.com/';
    }

    return null;
  }

  private async sendChatMessage(message: string, appendUserMessage: boolean): Promise<void> {
    this.isSending.set(true);
    this.isWaitingForFirstToken.set(true);
    this.errorMessage.set(null);
    this.failedChatMessage.set(null);

    if (appendUserMessage) {
      this.appendChatMessage({
        id: crypto.randomUUID(),
        sender: 'user',
        text: message,
        createdAtUtc: new Date().toISOString()
      });
    }

    this.messageText.set('');

    const shiroMessageId = crypto.randomUUID();
    this.appendChatMessage({
      id: shiroMessageId,
      sender: 'shiro',
      text: '',
      createdAtUtc: new Date().toISOString()
    });

    const request: ChatRequest = {
      conversationId: this.conversationId(),
      message
    };

    try {
      await this.streamChatResponse(request, shiroMessageId);
      this.lastFailedUserMessage = null;
      this.loadConversations();
    } catch (error) {
      this.removeChatMessage(shiroMessageId);
      this.lastFailedUserMessage = message;
      this.failedChatMessage.set(this.buildChatFailureMessage(error));
    } finally {
      this.isSending.set(false);
      this.isWaitingForFirstToken.set(false);
      this.focusMessageInput();
    }
  }

  private async streamChatResponse(request: ChatRequest, shiroMessageId: string): Promise<void> {
    const response = await fetch(`${apiBaseUrl}/chat/stream`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(request)
    });

    if (!response.ok || !response.body) {
      throw new Error(`Streaming request failed with HTTP ${response.status}.`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
      const { done, value } = await reader.read();

      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });
      const parts = buffer.split('\n\n');
      buffer = parts.pop() ?? '';

      for (const part of parts) {
        this.handleStreamEvent(part, shiroMessageId);
      }
    }

    if (buffer.trim()) {
      this.handleStreamEvent(buffer, shiroMessageId);
    }
  }

  private handleStreamEvent(rawEvent: string, shiroMessageId: string): void {
    const eventName = rawEvent
      .split('\n')
      .find(line => line.startsWith('event:'))
      ?.replace('event:', '')
      .trim();
    const dataText = rawEvent
      .split('\n')
      .filter(line => line.startsWith('data:'))
      .map(line => line.replace('data:', '').trim())
      .join('');

    if (!eventName || !dataText) {
      return;
    }

    const data = JSON.parse(dataText);

    if (eventName === 'meta') {
      this.conversationId.set(data.conversationId);
      return;
    }

    if (eventName === 'delta') {
      this.isWaitingForFirstToken.set(false);
      this.updateChatMessageText(shiroMessageId, currentText => `${currentText}${data.text ?? ''}`);
      return;
    }

    if (eventName === 'response' || eventName === 'done') {
      const chatResponse = data as ChatResponse;
      this.isWaitingForFirstToken.set(false);
      this.conversationId.set(chatResponse.conversationId);
      this.lastResponse.set(chatResponse);
      this.updateChatMessage(shiroMessageId, {
        text: chatResponse.message,
        createdAtUtc: chatResponse.createdAtUtc,
        response: chatResponse
      });
      this.refreshAfterChatResponse(chatResponse);
    }
  }

  private updateChatMessageText(messageId: string, update: (currentText: string) => string): void {
    this.chatMessages.update(messages =>
      messages.map(message =>
        message.id === messageId
          ? { ...message, text: update(message.text) }
          : message
      )
    );
    this.scrollMessageToTop(messageId);
  }

  private updateChatMessage(messageId: string, changes: Partial<ChatMessage>): void {
    this.chatMessages.update(messages =>
      messages.map(message =>
        message.id === messageId
          ? { ...message, ...changes }
          : message
      )
    );
    this.scrollMessageToTop(messageId);
  }

  private appendChatMessage(message: ChatMessage): void {
    this.chatMessages.update(messages => [...messages, message]);
    this.scrollMessageToTop(message.id);
  }

  private removeChatMessage(messageId: string): void {
    this.chatMessages.update(messages => messages.filter(message => message.id !== messageId));
  }

  private refreshAfterChatResponse(response: ChatResponse): void {
    if (response.responseType === 2) {
      this.loadTasks();
      this.loadAudit();
      return;
    }

    if (response.responseType === 3) {
      this.loadPendingApprovals();
      this.loadAudit();
    }
  }

  protected decideApproval(approvalId: string, decision: 'approve' | 'reject'): void {
    this.errorMessage.set(null);

    this.http.post(`${apiBaseUrl}/approvals/${approvalId}/${decision}`, {}).subscribe({
      next: () => this.refreshAll(),
      error: error => {
        this.errorMessage.set(this.buildApiErrorMessage(`${decision} approval ${approvalId}`, error));
      }
    });
  }

  protected refreshAll(): void {
    this.isRefreshing.set(true);
    this.errorMessage.set(null);
    this.loadPendingApprovals();
    this.loadConversations();
    this.loadTasks();
    this.loadAudit(() => this.isRefreshing.set(false));
  }

  private loadPendingApprovals(): void {
    this.http.get<ApprovalRecord[]>(`${apiBaseUrl}/approvals/pending`).subscribe({
      next: approvals => this.pendingApprovals.set(approvals),
      error: error => this.errorMessage.set(this.buildApiErrorMessage('load pending approvals', error))
    });
  }

  private loadConversations(): void {
    this.http.get<ConversationSummary[]>(`${apiBaseUrl}/conversations`).subscribe({
      next: conversations => this.conversations.set(conversations),
      error: error => this.errorMessage.set(this.buildApiErrorMessage('load chat history', error))
    });
  }

  private loadTasks(): void {
    this.http.get<ShiroTask[]>(`${apiBaseUrl}/tasks`).subscribe({
      next: tasks => this.tasks.set(tasks),
      error: error => this.errorMessage.set(this.buildApiErrorMessage('load tasks', error))
    });
  }

  private loadAudit(onComplete?: () => void): void {
    this.http.get<AuditLogEntry[]>(`${apiBaseUrl}/audit`).subscribe({
      next: entries => this.auditEntries.set(entries),
      error: error => {
        this.errorMessage.set(this.buildApiErrorMessage('load audit history', error));
        onComplete?.();
      },
      complete: () => onComplete?.()
    });
  }

  private buildApiErrorMessage(action: string, error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return `Unable to ${action}. Check API CORS and that Shiro.Api is running on ${apiBaseUrl}.`;
      }

      return `Unable to ${action}. API returned HTTP ${error.status}.`;
    }

    if (error instanceof Error) {
      return `Unable to ${action}. ${error.message}`;
    }

    return `Unable to ${action}.`;
  }

  private buildChatFailureMessage(error: unknown): string {
    if (error instanceof Error && error.message.includes('HTTP 404')) {
      return 'Shiro streaming is not available yet. Restart Shiro.Api, then retry this message.';
    }

    return 'Shiro could not send that message. Check that the API is running, then retry.';
  }

  private focusMessageInput(): void {
    setTimeout(() => this.messageInput?.nativeElement.focus(), 0);
  }

  private scrollMessageToTop(messageId: string): void {
    requestAnimationFrame(() => {
      const conversation = this.conversationPanel?.nativeElement;
      const target = this.chatMessageElements
        ?.find(element => element.nativeElement.dataset['messageId'] === messageId)
        ?.nativeElement;

      if (!conversation || !target) {
        return;
      }

      conversation.scrollTo({
        top: target.offsetTop - conversation.offsetTop,
        behavior: 'smooth'
      });
    });
  }
}
