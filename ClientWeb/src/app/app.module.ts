import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { SoundboxMainPage } from '../pages/SoundboxMainPage';
import { soundboxProvider } from '../providers/SoundboxProvider';

@NgModule({
    declarations: [
        AppComponent,
        SoundboxMainPage
    ],
    imports: [
        BrowserModule,
        AppRoutingModule,
        FormsModule
    ],
    providers: [
        soundboxProvider
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
