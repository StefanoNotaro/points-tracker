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
  templateUrl: './tournament-detail.component.html',
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
