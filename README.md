# Soundbox
Soundbox for TeamSpeak: the Soundbox server joins a TeamSpeak server like a normal client. However it only plays sound effects that are piped into TeamSpeak (via "Stereo mix" input etc.). The Soundbox server is remote controlled by the Soundbox clients (Desktop/browser/mobile app)

# Wait, what?
Obviously it can be a lot of fun playing audio clips on Voice-over-IP clients like TeamSpeak; just like it can be a lot of fun sharing memes in text chats.

Problem is: TeamSpeak does not offer this kind of function. It only relays your audio input (so usually your microphone) to the other users in the channel. There are some ways to get audio clips into TeamSpeak, for example:
* Using something like https://www.vb-audio.com/Cable/
* A TeamSpeak addon like https://www.myteamspeak.com/addons/9e5d66d9-b951-4f46-9b08-0e62909235ee

The first solution is too hard to set up for most users and both have three key problems:
1. Users need to install something on their machine
1. Every user needs to curate their own collection of sounds
1. The audio runs over the same "transmission channel" as your voice. Meaning other users in the same channel cannot opt-out of hearing those clips without muting you entirely.

Enter the Soundbox:

The Soundbox server runs on its own physical or virtual machine and joins a TeamSpeak server using the official client like a normal user.
The Soundbox's TeamSpeak client and OS are set up in such a way that:
* TeamSpeak's entire output is muted
* TeamSpeak's audio input is actually the output of another software

  There are two ways to get that going. Either...
  1. Use a built-in driver function like "Stereo mix" or "what you hear".
That is a virtual audio input device where your machine's entire audio output is turned into an input. Thus selecting that device in TeamSpeak as audio source will cause anything you hear locally to be transferred to your mates in TeamSpeak.
  2. Use a software like https://www.vb-audio.com/Cable/ . With such a solution you would select a virtual audio cable as output for your media player like VLC. Then select the other end of that virtual audio cable as input in TeamSpeak. This causes VLC's output to be sent to TeamSpeak as input.

What clips the Soundbox Server is playing is controlled from a web app where everyone can upload and share their sounds.
Once set up by a hoster it's easy for users to join the fun...or to opt-out of it by muting the Soundbox client in their TeamSpeak.

# Functions

Other than the obvious (that is, playing sounds) the Soundbox offers the following features:
* Volume control, both global and on a per-sound basis
* A global Stop button
* Sound effects. For now that is only a pitch-and-speed shifter
* Sound macros giving a button more functionality:
  1. Sound chains on the server-side: Let your "Hello there" always be accompanied by a "General Kenobi"
  2. Sound sequences: Each button press plays the next sound in a pre-defined list of sounds
  3. Random sounds: Each button press plays a random sound from a pre-defined set of sounds
* Directories to manage sounds more easily
* Tags to categorize and find sounds after some criteria (e.g. "Star Wars")
* A mobile-first web client
* That same web client embedded in a desktop app to enable global key bindings

Well, at least that's what is planned:

# Current state of the project

The server's prototype stage is pretty much done and works well when hosted in Windows.

The frontend is basically non-existent yet, only a plain and simple test UI exists.

# Running the Soundbox

Open the project in Visual Studio and run it or publish it.
When running in IIS the worker process needs full access to the directory where the Soundbox should keep its database and the uploaded sounds.
Currently that is the "wwwroot" directory within the application's directory.

Upload a sound and play it. You will the hear sound being played on your machine.

To get that sound into TeamSpeak you should take a look at the the [introduction](#wait-what)

*Note*: on Windows playing .mp3 and .flac doesn't work yet when running on IIS Express (i.e. running or debugging from within Visual Studio)

# Other VoIP clients

So far it's been all about TeamSpeak as that is my usual VoIP client of choice.
However the Soundbox can just as well be used with other programs as well.

I suppose Discord would be the obvious next candidate to implement.
Discord has the very lovely advantage that it offers an API to send audio data directly from the Soundbox process to a Discord server. TeamSpeak does not offer this function which is why the Soundbox server must run a TeamSpeak client to talk to the TeamSpeak server.

# Technologies

Backend:
* ASP.NET Core 3.1
* Database: LiteDB (https://www.litedb.org/)

  Stores meta data per sound such as the sound's name, file name, play length, tags...

* Sound playback:
  * IrrKlang (https://www.ambiera.com/irrklang/) on Windows. Had actually wanted to use libvlc (https://github.com/videolan/libvlcsharp) but it absolutely refused to play certain file types like .mp3 and .ogg

Communication:
* SignalR (https://docs.microsoft.com/de-de/aspnet/core/signalr/introduction?view=aspnetcore-3.1)

  For most of the communication: telling the server what sounds to play, receiving events from the server (e.g. what sounds are playing right now)
* REST API to upload sounds

Frontend (planned)
* Typescript
* Probably Angular (https://angular.io/) and Bootstrap (https://getbootstrap.com/)
* Desktop client will probably use Electron (https://www.electronjs.org/) to wrap the web client

# Getting to know the code

Main code file for the backend is in the "Server" project: soundbox/Soundbox.cs

Management of the uploaded files for example is done directly in there, other functions such as playing sounds and talking to the database are delegated away to other classes via dependency injection.
See the Startup.cs for which class provides which function at runtime.

Soundbox.cs is called from soundbox/SoundboxHub.cs which is the SignalR Hub. That's where you can see the functions users can call from the web client
