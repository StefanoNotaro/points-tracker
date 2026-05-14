import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'pts-access-denied',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './access-denied.component.html',
})
export class AccessDeniedComponent {}
