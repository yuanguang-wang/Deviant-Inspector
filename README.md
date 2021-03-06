# Deviant Inspector

## General
<img align="left" src="/EmbeddedResources/deviant.png" width="300" height="300"> 

### Description
Deviant Inspector is a Rhino Plugin helping to diagnose the breps in the rhinodoc.3dm file, distinguishes the breps which have some specific geometry characteristics; once some of the breps' attributes match the preset diagnose profiles, those breps will be colored and given a name of the matched preset diagnose profiles. 

### System Requirement
Rhino 7.0 and later, Windows 10/11. <br/>
Support MacOS theoretically but never tested.<br/>

<br clear="left"/>

The preset diagnose proflies, as well as the specific geometry characteristics are the core functions of this plugin, 
including "Curl", "Vertical", "Extrusion", "Redundancy" standing for different geometry condition listing below.

The purpose of this plugin is to detect the brep made unintendedly, 
lacking accuracy to whether make2d to draw cad file or convert to revit as generic model.
<br/>
<br/>

## Instillation
### - via Package Manager
- Open *Rhino*
- In the Command Window, type the command ```PackageManager```
- In the poped up *Package Manager* window, search ***Deviant-Inspector***
- Click Install
- Restart Rhino

### - via Food4Rhino
Click the link below to download via food4rhino:
<br/>
[https://www.food4rhino.com/en/app/deviant-inspector](https://www.food4rhino.com/en/app/deviant-inspector)
<br/>
<br/>

## Commands
Deviant Inspector has two embedded commands that could be typed in the Rhino Command Window.

#

<img align="left" src="/EmbeddedResources/deviant-01.png" width="150" height="150"> 

### ***devin***
"***devin***" stands for "**Deviant Inspection**", by typing "devin" in rhino command window, pick a color and select the breps and blocks need to be diagnosed, the 
command will run, by marking them with the picked color and the associated name.
<br clear="left"/>

#

<img align="left" src="/EmbeddedResources/deviant-02.png" width="150" height="150">

### ***devro***
"***devro***" stands for "**Deviant Rollback**", by typing "devro" in rhino command window, select the breps and blocks need to weep out those changes made by the 
command "devin", and then all the changes will be rolled back to make the model looks like the original.
<br clear="left"/>
<br/>

## Terminology
### Curl
"***Curl***" means that a surface (alone or contained in a polysurface) is visually flat but actually not if tested by the ```rs.IsSurfacePlanar()```;
```rs.IsSurfacePlanar()``` allows the ModelTolerance level of precision to decide whether a surface is flat or not. 
<br/>
By opening "Curl" toggle on, the plugin will iterate every brep selected to mark the deviants with color and add a **[Curl]** at the objects' name.
<img src="/EmbeddedResources/diagram-01.png"> 
### Vertical
"***Vertical***" means that a surface is visually vertical in rhino space, parallel with the z-axis but actually not, such as have a 0.05 degree with the z-axis.
This function will detect the surfaces which have only a small angle from the vertical / z-axis.
<br/>
By opening "Vertical" toggle on, the plugin will iterate every brep selected to mark the deviants with color and add a **[Vertical]** at the objects' name.
<img src="/EmbeddedResources/diagram-02.png"> 
### Extrusion
"***Extrusion***" means that a surface is extruded by dragging the gumball, in the condition that a line is extruded in the direction of itself,
meaning the direction vector is the connection of the start and end point. 
Doing this, the surface has its four (or more) boundary vertexes co-liner and so it looks like a line.
<br/>
By opening "Extrusion" toggle on, the plugin will iterate every brep selected to mark the deviants with color and add a **[Extrusion]** at the objects' name.
<img src="/EmbeddedResources/diagram-03.png"> 
### Redundancy
"***Redundancy***" means that a surface could be simplified to has less control point so as to reduce the model's size. 
The simplification is working on the boundary curves of each surface, to test whether each boundary could be simplified or not.
Core API used behind this method is the ```Rhino.Geometry.Curve.Simplify()``` .
Hand-made rhino model has more difficulty to reach the goal that every brep is clean and simplest while doing design study, so by default this toggle is turned off.
<br/>
By opening "Redundancy" toggle on, the plugin will iterate every brep selected to mark the deviants with color and add a **[Redundancy]** at the objects' name.
<img src="/EmbeddedResources/diagram-04.png"> 
### Block
"***Block***" means that whether the breps in a block will be diagnosed or not. 
The selection section will allow the selection of both brep and block, 
but if the toggle is turned to "exclude", the block will be selected but never diagnosed,
so, no worry about turns on the selection filter to prevent block changing.
the block will change only the toggle is turned to "include", and diagnose blocks will increase the diagnose time apparently,
so, by default the block toggle is turned to "exclude".
<br/>
<br/>

## Warning
To avoid long reading, a short warning session is prepared to condense the conditions need to keep eyes on:
- Diagnose blocks will increase the diagnose time depending on how many block definitions in the model.
- Diagnose Operation will break the build history.
- "***devro***" is not equal to ```_undo``` command, it could work independently without using "devin" command ahead.
<br/>

## Epilogue
I just hope you never have a situation that need to use this plugin :)
<br/>
Enjoy your blaming time :)
