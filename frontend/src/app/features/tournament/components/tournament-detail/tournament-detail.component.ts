import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TournamentService } from '../../services/tournament.service';
import { TournamentHubService } from '../../services/tournament-hub.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { BracketViewComponent } from '../bracket-view/bracket-view.component';
import { ParticipantsManagerComponent } from '../participants-manager/participants-manager.component';
import { TournamentSettingsComponent } from '../tournament-settings/tournament-settings.component';
import { Tournament, TournamentMatch, minTeamsForFormat } from '../../../../shared/models/tournament.model';

type Tab = 'bracket' | 'participants' | 'standings' | 'settings';

@Component({
  selector: 'pts-tournament-detail',
  imports: [LoadingSpinnerComponent, BracketViewComponent, ParticipantsManagerComponent, TournamentSettingsComponent, TranslatePipe],
  template: `
    @if (loading()) {
      <div class="flex items-center justify-center py-20"><pts-loading-spinner size="lg" /></div>
    } @else if (tournament(); as t) {
      <div class="flex flex-col gap-5 pb-8">
        <!-- Header -->
        <header class="flex items-start justify-between gap-2">
          <div class="flex flex-col gap-1 min-w-0">
            <p class="text-xs uppercase tracking-wide text-on-surface-muted">
              {{ 'sport.' + t.sportType | translate }} · {{ 'tournament.format.' + t.format + '.label' | translate }}
            </p>
            <h1 class="text-xl sm:text-2xl font-bold text-on-surface truncate">{{ t.name }}</h1>
            <p class="text-sm text-on-surface-muted">
              {{ 'tournament.detail.teamsAndStatus' | translate: { count: t.participants.length, status: ('tournament.status.' + t.status | translate) } }}
            </p>
          </div>
          <button type="button" class="pts-btn-icon shrink-0"
                  (click)="copyShareLink()"
                  [attr.aria-label]="'tournament.detail.shareAria' | translate"
                  [attr.title]="'tournament.detail.shareTitle' | translate">
            <span class="material-symbols-rounded text-xl">share</span>
          </button>
        </header>

        <!-- Actions -->
        @if (t.canEdit && t.status === 'draft') {
          <div class="flex flex-col gap-1">
            <button type="button" class="pts-btn-primary"
                    [disabled]="t.participants.length < minTeams() || starting()"
                    (click)="start()">
              <span class="material-symbols-rounded text-lg">play_arrow</span>
              <span>{{ (starting() ? 'tournament.detail.starting' : 'tournament.detail.start') | translate }}</span>
            </button>
            @if (t.participants.length < minTeams()) {
              <p class="text-xs text-on-surface-muted text-center">
                {{ 'tournament.detail.startHelpMin' | translate: { min: minTeams() } }}
              </p>
            }
          </div>
        }

        <!-- Tabs -->
        <nav class="flex gap-1 border-b border-border">
          @for (tab of tabs; track tab.id) {
            <button type="button"
                    class="px-3 py-2 text-sm font-semibold border-b-2 -mb-px transition-colors"
                    [class.border-primary]="activeTab() === tab.id"
                    [class.text-primary]="activeTab() === tab.id"
                    [class.border-transparent]="activeTab() !== tab.id"
                    [class.text-on-surface-muted]="activeTab() !== tab.id"
                    (click)="activeTab.set(tab.id)">
              {{ 'tournament.detail.tabs.' + tab.id | translate }}
            </button>
          }
        </nav>

        @switch (activeTab()) {
          @case ('bracket') {
            @if (t.matches.length === 0) {
              <div class="pts-card text-center py-8 text-sm text-on-surface-muted">
                {{ 'tournament.detail.bracketEmpty' | translate }}
              </div>
            } @else {
              <pts-bracket-view [matches]="t.matches" [canEdit]="t.canEdit"
                                (matchClicked)="openMatch($event)" />
            }
          }
          @case ('participants') {
            <pts-participants-manager [tournamentId]="t.id"
                                      [participants]="t.participants"
                                      [canEdit]="t.canEdit && t.status === 'draft'"
                                      [format]="t.format"
                                      [groupCount]="t.rules.groupCount"
                                      (changed)="refresh()"
                                      (shuffleChanged)="shuffleUnseeded.set($event)" />
          }
          @case ('settings') {
            @if (t.canEdit) {
              <pts-tournament-settings [tournament]="t" (saved)="tournament.set($event)" />
            } @else {
              <div class="pts-card text-center py-8 text-sm text-on-surface-muted">
                {{ 'tournament.detail.settingsLocked' | translate }}
              </div>
            }
          }
          @case ('standings') {
            @if (t.standings.length === 0) {
              <div class="pts-card text-center py-8 text-sm text-on-surface-muted">
                {{ 'tournament.detail.standingsEmpty' | translate }}
              </div>
            } @else {
              <ul class="flex flex-col gap-1">
                @for (s of t.standings; track s.participantId; let i = $index) {
                  <li class="pts-card !p-3 flex items-center gap-3">
                    <span class="font-mono text-on-surface-muted w-6 text-center">{{ i + 1 }}</span>
                    <span class="flex-1 min-w-0 truncate text-on-surface font-medium">{{ s.teamName }}</span>
                    <span class="font-mono text-sm">
                      <span class="text-success">{{ s.wins }}</span>
                      <span class="text-on-surface-muted">–{{ s.losses }}</span>
                    </span>
                  </li>
                }
              </ul>
            }
          }
        }
      </div>
    }
  `,
})
export class TournamentDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(TournamentService);
  private readonly hub = inject(TournamentHubService);
  private readonly notifications = inject(NotificationService);
  private readonly i18n = inject(TranslateService);

  readonly loading = signal(true);
  readonly tournament = signal<Tournament | null>(null);
  readonly starting = signal(false);
  readonly activeTab = signal<Tab>('bracket');
  readonly shuffleUnseeded = signal(false);

  readonly minTeams = computed(() => {
    const t = this.tournament();
    if (!t) return 2;
    return minTeamsForFormat(t.format, t.rules.groupCount);
  });

  readonly tabs: { id: Tab }[] = [
    { id: 'bracket' },
    { id: 'participants' },
    { id: 'standings' },
    { id: 'settings' },
  ];

  private id = '';
  private joined = false;
  private subs: Subscription[] = [];

  async ngOnInit(): Promise<void> {
    this.id = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.id) {
      void this.router.navigate(['/tournaments']);
      return;
    }
    await this.refresh();
    this.subs.push(this.hub.tournamentUpdated$.subscribe((t) => {
      if (t.id === this.id) this.tournament.set(t);
    }));
    try {
      await this.hub.joinTournament(this.id);
      this.joined = true;
    } catch { /* live updates optional */ }
  }

  async ngOnDestroy(): Promise<void> {
    for (const s of this.subs) s.unsubscribe();
    if (this.joined) {
      try { await this.hub.leaveTournament(this.id); } catch { /* ignore */ }
    }
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try { this.tournament.set(await this.service.getById(this.id)); }
    catch { this.notifications.error(this.i18n.instant('tournament.detail.loadError')); }
    finally { this.loading.set(false); }
  }

  async start(): Promise<void> {
    this.starting.set(true);
    try {
      this.tournament.set(await this.service.start(this.id, { randomizeUnseeded: this.shuffleUnseeded() }));
      this.activeTab.set('bracket');
    } catch (err: any) {
      this.notifications.error(err?.error?.detail ?? this.i18n.instant('tournament.detail.startError'));
    } finally {
      this.starting.set(false);
    }
  }

  async openMatch(m: TournamentMatch): Promise<void> {
    if (m.counterId) {
      await this.router.navigate(['/counter', m.counterId]);
      return;
    }
    try {
      const counter = await this.service.openMatchCounter(this.id, m.id);
      await this.router.navigate(['/counter', counter.id]);
    } catch (err: any) {
      this.notifications.error(err?.error?.detail ?? this.i18n.instant('tournament.detail.openMatchError'));
    }
  }

  async copyShareLink(): Promise<void> {
    try {
      await navigator.clipboard.writeText(window.location.href);
      this.notifications.success(this.i18n.instant('tournament.detail.shareCopied'));
    } catch {
      this.notifications.error(this.i18n.instant('tournament.detail.shareFailed'));
    }
  }

}
