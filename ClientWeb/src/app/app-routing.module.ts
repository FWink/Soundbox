import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { SoundboxMainPage } from '../pages/SoundboxMainPage';


const routes: Routes = [
    {
        path: '', component: SoundboxMainPage, pathMatch: 'full'
    }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
