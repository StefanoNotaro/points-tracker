import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'pts-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {}
