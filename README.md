# Deviant Inspector

## Before Start
### What is it
This is a Plugin surving a purpose of analysing, which is not about to create, modify or delete objects in the original Rhino document. So the next question would be: Analyze what? This PlugIn is showing user some inapparent information that may not noticed before.
For instance, whether on purpose or not, someone did a vertical extrusion, but the workplane which the extrusion is depended on has a little angle with the world top, like 89.5. In result, the extrusion is not vertical, but human's eye can't see the difference.
### Why Bother?
It's ok to keep the nearly vertical extrusion for most of the situations, like the vertical angle is already in the tolerance, rhino will still judge it as a vertical extrusion; the model is sent to the visualizer and the fancy render shows nothing wierd, because human's eye can't see the little angle change; the model is make2d to AutoCad and drawn to Construction Document, built on site and the workers on site would manully revise the little diagnal, nothing effected.
So why bother? It's about effeciency. Imagine that the CAD drawer is trying to catch a perpendicular snap on a make2ded extrusion's line, but it's not 90, so the new line drawn is not horizontal apperently. The small issues will cascade till a noticable diagnal is shown, then it maybe a bit late to change all invalid line had been drawn.

