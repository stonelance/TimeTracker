# TimeTracker

# Overview
TimeTracker is a Windows desktop application I originally developed to be able to track my own time at work.  I felt like I was wasting a lot of time task switching while waiting for very long build times, and I wanted a way to quantify that loss of time.
The app tracks what programs have focus, or are actively using CPU processing and maps those to a configurable list of activities, which is then used to track how much time is spent on various activities.  Several years after developing the app I was
spending more time working from home, and I needed a way for my family to know if I was working and couldn't be interrupted or if it was ok for them to come into my office.  At that point I updated TimeTracker to support more extensibility via plugins,
and implemented a plugin to control a Twinkly Square LED light which I mounted outside my home office.  The Twinkly functionality is completely optional, and can be disabled if anyone doesn't need that functionality.

# How to build TimeTracker
This project is currently a .NET 8.0 application build in VS 2022.  After syncing you shoudl be able to build TimeTracker project in either Debug or Release and all relevent actions should be taken automatically and the application should be runnable.

# How to use TimeTracker
The default configuration is in defautlConfiguration.json.  On first run of TimeTracker this file will get copied to %localappdata%\TimeTracker\configuration.json.  This directory also contains the activity log that is tracked by the app each day.
The default configuration should already contain many common activities and applications, and serve as an example should you need to modify any configuration.  While the app is running, you can leave it in the background and it will track what
activities are happening on your machine.  The top of the window contains a timeline view of what activities have been tracked at any given time.  You can choose to show or collapse Away time in the timeline, and you can scroll and zoom the timeline
view with the mouse.  You can also change what date range is being displayed in the app.  The default is to only show the current day.  Below the timeline on the left shows the current activity tracked by the app.  On the bottom right shows a summary
of all time spent in each activity in terms of absolute time and relative time (excluding away\idle).

![Acreenshot of TimeTracker](/Screenshot.jpg)

# Plugin Types
A given dll can only implement one of these various types of plugin.

### Tracker Plugin
This plugin allows implementing a class derrived from TimeTracker.ITrackerPlugin to monitor and respond to changes in the current activity.

### Watcher Plugin
This plugin allows implementing a custom watcher by implementing a class that derrives from TimeTracker.BaseWatcher.

# Configuration format
The configuration file is in json format with three main sections:

## Plugins
This section lists the plugins based on relative or absolute path to the dll.  It can also contain optional parameters depending on the plugin.  TimeTracker comes with two plugins:

### - TwinklyPlugin
This is a Tracker plugin, which uses the current activity events to control the state of a Twinkly Square LED light.  This plugin has one property DeviceIPOrHostName, which specifies the address of the Twinkly device on the local network.
It also has one custom property for Activities - LedColor, which is used to specify the color to display on the LED lights when that activity is active.

### - GameFocusWatcher
This is a Watcher plugin uses Windows MRU game list to determine if a running process is a game, and can be used to automatically classify a large number of gaming applications without having to explicitly list them all in the config file.

## Activities
This section allows specifying all the activities the app will track.  Some activities such as NoData, Idle and Away are built in and will automatically be added if not included in the list.  This section allow choosing the color used by the activity in
the UI.  Tracker plugins can also have custom settings per activity specified under PluginSettings.  For example, the Twinkly plugin specifies the LEDColor for each activity.

## Watchers
This section defines all watchers used by the app.  The watchers determine when a given activity is occuring or not.  Multiple watchers can be mapped to the same activity (for instance, both chrome nad edge could be classified as "Browsing").  Watchers
are specified by their Type, and then have various parameters depending on the type of watcher.

### Types of Watchers
There are a few different watchers built in to TimeTracker, and one that is provided as a plugin

#### - LockScreenWatcher
This is used to determine if the machine is locked and the user is away

#### - InputWatcher
This is used to determine if the user is idle or has walked away from their machine by monitoring keyboard and mouse inputs

#### - ProcessFocusWatcher
This is used to determine if the given process is currently in the foreground process

#### - ProcessActivityWatcher
This is used to determine if a process is using a large amount of CPU

#### - GameFocusWatcher (via Plugin)
This is a custom version of the ProcessFocusWatcher which checks the process name against the recently played game list that Windows writes in %localappdata%\Microsoft\GameDVR\GameMRU\LocalMruGameList.json.

### Shared Properties
All watchers have these properties:

#### - DisplayName
The name of the watcher displayed in the bottom left of the app UI

#### - ActivityName
Name of the activity that should be active when this watcher is active

### Other Properties
These other properties are used dependign on the watcher type

#### - ProcessName
For process related watchers, this specifes the name of the exe (excluding extension) that the watcher tracks.

#### - UpdatePeriodInSeconds
For watchers that use polling mechanisms, this specifies how often to poll

#### - CPUUsageThresholdForRunning
For the ProcessActivityWatcher, this specifies how much CPU utilization is required to consider the process active

#### - DelayBeforeReturnToInactiveInSeconds
For the ProcessActivityWatcher, this specifies how long to wait for CPU utlization to go below the threshold before considering the process inactive

#### - TimeToIdleInSeconds
For the InputWatcher, this specifies for how long there must be no user input to consider the InputWatcher active (ie. idle)

## Example
    {
      "Plugins": [
        {
          "Path": "TwinklyPlugin.dll",
          "DeviceIPOrHostName": "Twinkly-D9C649"
        },
        {
          "Path": "GameFocusWatcher.dll"
        }
      ],
      "Activities": [
        {
          "Name": "Unknown",
          "Color": "#FFFF0000",
          "PluginSettings": [
            {
              "PluginName": "TwinklyPlugin",
              "LedColor": "#FF008000"
            }
          ]
        },
        {
          "Name": "Internet",
          "Color": "#FF00FFFF",
          "PluginSettings": [
            {
              "PluginName": "TwinklyPlugin",
              "LedColor": "#FF008000"
            }
          ]
        },
        {
          "Name": "Idle",
          "Color": "#FFFFC0CB",
          "PluginSettings": [
            {
              "PluginName": "TwinklyPlugin",
              "LedColor": "#FF000000"
            }
          ]
        },
        {
          "Name": "Away",
          "Color": "#FF87CEFA",
          "PluginSettings": [
            {
              "PluginName": "TwinklyPlugin",
              "LedColor": "#FF000000"
            }
          ]
        },
        {
          "Name": "Compiling",
          "Color": "#FF800080",
          "PluginSettings": [
            {
              "PluginName": "TwinklyPlugin",
              "LedColor": "#FF800000"
            }
          ]
        },
        {
          "Name": "Gaming",
          "Color": "#FF8080FF",
          "PluginSettings": [
            {
              "PluginName": "TwinklyPlugin",
              "LedColor": "#FF808000"
            }
          ]
        }
      ],
      "Watchers": [
        {
          "Type": "LockScreenWatcher",
          "DisplayName": "Lock Screen",
          "Activityname": "Idle"
        },
        {
          "Type": "InputWatcher",
          "DisplayName": "Idle",
          "Activityname": "Idle",
          "TimeToIdleInSeconds": 60,
          "UpdatePeriodInSeconds": 1
        },
        {
          "Type": "GameFocusWatcher",
          "DisplayName": "Game",
          "ActivityName": "Gaming"
        },
        {
          "Type": "ProcessFocusWatcher",
          "DisplayName": "Chrome",
          "ActivityName": "Internet",
          "ProcessName": "chrome"
        },
        {
          "Type": "ProcessActivityWatcher",
          "DisplayName": "Compiler",
          "ActivityName": "Compiling",
          "ProcessName": "cl",
          "CPUUsageThresholdForRunning": 0.02,
          "DelayBeforeReturnToInactiveInSeconds": 0.0,
          "UpdatePeriodInSeconds": 0.5
        }
      ]
    }