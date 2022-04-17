import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { SoundboxMainPage } from '../pages/SoundboxMainPage';
import { SoundboxRandomPage } from '../pages/random/SoundboxRandomPage';
import { SoundboxStopPage } from '../pages/stop/SoundboxStopPage';


const routes: Routes = [
    {
        path: '', component: SoundboxMainPage, pathMatch: 'full'
    },
    {
        path: 'random', component: SoundboxRandomPage, pathMatch: 'full'
    },
    {
        path: 'stop', component: SoundboxStopPage, pathMatch: 'full'
    }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
