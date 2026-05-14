import { Component, output, input } from '@angular/core';
import { NgClass } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { SportConfig, SportType } from '../../models/sport.model';

@Component({
  selector: 'pts-sport-selector',
  imports: [NgClass, TranslatePipe],
  templateUrl: './sport-selector.component.html',
})
export class SportSelectorComponent {
  sports       = input.required<SportConfig[]>();
  selected     = input<SportType | null>(null);
  sportSelected = output<SportType>();
}
