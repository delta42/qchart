# QChart

**QChart** is a Windows application that allows visualization of Quake match dynamics using as input an MVD demo file. It's a .NET WinForms application that leverages both [MVDparser](https://github.com/QW-Group/mvdparser) and the [Windows Chart forms control](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datavisualization.charting?view=netframework-4.8).

This is a sample output from the application:

![Sample Output](sampleoutput.png)

## How To

To run **QChart** you just need to download the latest installer from the [releases](https://github.com/delta42/qchart/releases) page. The download link will present itself under the list of assets as `QChart.zip`. Simpy download and unzip it and run `setup.exe`. If your don't already have .NET Desktop Runtime it will be downloaded and installed as a first step. Then the app will be quickly installed (to `\Program Files\delta42\QChart`), and a shortcut to `QChart` added to your Desktop.

Then all you need to do is run `QChart` and drag and drop an MVD file onto the app window, or double-click the app window to choose an MVD file by browsing to it. The chart will then get created and displayed.

## The Future

Here are some short term goals.

* Add a graph series to indicate team score spread, to give an idea of what team is winning and by how much at any given time.
* Research how we can incorporate deaths into the chart data.
* Research how other interesting metadata can be visualized in the same chart: e.g., Quad, Pent.
* Add more error checks to catch unexpected inputs - so far testing has been focussed on a handful of 4 on 4 matches.
* Do testing for 1 on 1 and 2 on 2 matches.
* Do testing for FFA MVD files and add support for them.
* Modify `mvdparser.exe` to write files to the Windows temp folder and then delete them once they are no longer needed.
* Add a way for the user to save the chart out as a PNG file so that a sceen capture is not needed.
* Hopefully get feedback from users and incorporate new ideas!

## Current Design

* Once the user selects an MVD file, an `mvdparser.exe` process is spawned with the MVD path as a parameter. This will generate a series of log files in the same folder as the MVD file. We get the map name from `[MVDPath]-map.log` and the match date from `[MVDPath]-demo.log`. These are then used as the graph title.
* We then loop across all `[MVDPath]-[N]-events.log` files for values of N from 0 to 31: the data within forms the basis of the chart lines.
* The default `template.dat` file from MVDparser was modified to log FRAG events instead of DEATH event, so that now a typical even log will look like this:

```
matchtime=47.8742;player=8;frags=1;suicides=0;teamkills=0;type=FRAG;name=hangtime;team=muta
matchtime=84.9404;player=8;frags=2;suicides=0;teamkills=0;type=FRAG;name=hangtime;team=muta
matchtime=108.349;player=8;frags=3;suicides=0;teamkills=0;type=FRAG;name=hangtime;team=muta
...
```

* From each event log we create a `PlayerSession` instance that contains the player's Name, Team and an array of events containing the `matchtime`, `frags`, `suicides` and `teamkills`.
* Each of the `PlayerSessions` are then charted with matchtime on the X-Axis and `frags` on the Y-Axis.
* The X symbol on the chart identifies a `suicide` and the star symbol a `teamkill`. Note that it is possible to have multiple flags for an event; for example, one can have a `frag` and a `teamkill` simultaneously. For this reason we adopt a priority approach whereby `frags` have priority over `suicides` which have priority over `teamkills`, as far as the chart symbol used is concerned.
* Colors for each graph series are chosen from a list of fixed colors. The winning team is given colors from a blue-green palette while the losing team from a red-orange one.
* A legend is added to the chart listing all players, their line colors and final scores. The list is sorted by winning team and then highest final score.
