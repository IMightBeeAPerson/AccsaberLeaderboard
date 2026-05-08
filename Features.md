# Feature List
This will be all the features in my leaderboard, for merging purposes.

## Front End
This is the visual stuff, some part of the back end will have to be ported with this but it can be somewhat independent of back end.

### Panels

- Main leaderboard list (meaning the leaderboard cells and cell data)
- Main leaderboard buttons (meaning the page up, down, and top, go to player page button, and leaderboard selector buttons)
- Leaderboard header panel (the panel that says "Accsaber", star & mode ratings, and the rgb background)
- Panel (meaning the panel that shows overall stats as well as type stats, player profile image, and accsaber reloaded logo)
- Player score modal (meaning the popup that is shown when a player score is clicked)

### Modals

- Player profile modal (meaning the popup that is shown when the "view profile" button is pressed in the score modal)
- Milestone list modal (meaning the list of milestones that is shown when clicking the accsaber reloaded logo) 

## Back End
This is the stuff that lets the leaderboard work. Not really the connecting code, since that will have to be reworked no matter what, but more structural code.

### Systems

- API system (meaning the classes in the API folder, where everything is centuralized)
    - AccsaberAPI class (contains all the calling code to the api, as well as JToken pathing for all the responses from the api to get certain information)
    - APIHandler class (small class that contains the actual function to invoke the api. Made to be as robust as possible so no matter what it will not deadlock or crash)
    - AccsaberLiveScores class (websocket handler)
    - HelpfulPaths class (contains all endpoints used by the api, as well as some helper functions with converting types)
    - Throttler class (simple class to make sure the api is never called more than max allowed)

- BSML Addons (not entirely a system, but thought this was the best place to place this)
    - Components
        - CustomBackground (this a component that allows for a background of any image, without annoying dimmers, to be added)
        - MyCustomCellListTableData (the name is a bit of a lie, this is mainly a special cell that doesn't work with TableView, nor is scrollable. Mainly specialized for the leaderboard display)
        - MyEventSystemListener (like 3 lines of code that detect when a click happens)
    - Tags
        - Better vertical/horizontal (these are vertical/horizontal tags that have the Backgroundable component replaced with the CustomBackground component)
        - MyCustomList (This is a custom list that doesn't implement a TableView or Scrollable, just purely displays the cells)

### Models

- AccsaberMilestoneData class (this one contains JValues for a milestone cell to be displayed on the milestone list)
- AccsaberScoreData class (this one contains JValues for the leaderboard cell to be displayed on the main leaderboard)
- APCategory enum class (just an enum for True, Standard, Tech, and Overall)
- LeaderboardDisplayType enum class (a widely used bitmask enum for the display types of the leaderboard)
- LevelMilestone record class (this is a model for the milestone thresholds)


### Misc Classes

- ObjectCacher class (this class automatically handles caching and times out items after a given amount of time)
- AsyncLock class (this one allows for locking a function while also awaiting inside of the lock)
- PlayerSocialLife class (this class handles loading auth as well as providing the main player id and its relations)
- LangExtensions class (this class is to add attributes/small classes from later .NET into this project)
- ColorPalette class (this class contains all (or most) colors used in this leaderboard for ease of use)
