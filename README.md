# ANTShapes [Anomalous Neuromorphic Tool for Shapes]

_This project is currently in an open alpha state of development.  There are some missing and incomplete features for CPU-based rendering, and optimisation is needed.  Please feel free to try the software anyway and suggest features :)_

_Prefer not to download an executable?  Check out our example datasets, or the web demo! (Requires Unity player.)_

ANTShapes is a neuromorphic vision dataset simulator for anomaly detection.  It simulates scenes populated by 3D objects and renders event data in a similar manner to real-life Dynamic Vision Sensors (DVSs).

The tool was designed to address the lack of high-quality fully-labelled datasets for the training of Spiking Neural Networks (SNNs) for anomaly detection in computer vision as part of the [EDGY Organism](https://iotgarage.net/projects/EdgyOrganism.html) project.

Typically, a conventional video anomaly dataset (e.g. Shanghai Tech, Alleyway) would be converted into a spike-based representation by comparing the pixel intensity between high-speed interpolated frames or by pointing a DVS at a screen displaying the video footage.

Such approaches can fail to capture event data properly, if the source frame-rate for conversion is too low for effective interpolation, or if the capture from the DVS pointed at the screen is disturbed somehow.

Furthermore, these conversion methods do not address other problems of dataset depth, variety, complexity and anomaly labelling that may be present in the source datasets for conversion.

# Goal

The purpose of ANTShapes is to address these limitations.

We aim to develop an anomalous event simulator that is:

- **Simple:**  SNNs for anomaly detection currently perform worse than ANNs.  Anomalies and behaviours represented in ANTShapes are therefore constrained to simple actions like rotation and translation, and to simple shapes like cubes, spheres and cylinders.

- **Robust:**  An anomaly definition system based on central limit theorem powers ANTShapes.  Anomalies are defined statistically with pixel-perfect labelling for fully-supervised learning.

- **Configurable**:  Users are free to define anomalies from available behaviours however they please.  Simulated datasets can therefore be as simple or as complex as desired.

- **Flexible**:  Users are also given freedom to define rendering attributes: resolution, temporal scale, scene depth effects, background noise...

- **Deterministic:**  Simulations are repeatable and stable, regardless of the frame rate of the app and scene complexity.

# Objects and Behaviours

ANTShapes features 12 object classes:

- **Simple flat-faced geometries**:  Cuboids, icospheroids, pyramids, T-block and L-block "Tetris"-style objects

- **Simple smooth-faced geometries**:  Spheroids, cones, toruses, cylinders, capsules

- **Complex reference models**:  Utah teapot, Suzanne monkey head

Up to 1024 objects can be spawned in the scene.  Each object can exhibit the following properties:

- **Rotation speed**:  rotation around X, Y and/or Z axes

- **Translation speed**:  translation along X, Y and/or Z axes
 
- **Scale**:  object scale along X, Y and/or Z axes
 
- **Initial rotation**:  initial rotation around X, Y and/or Z axes
 
- **Initial position**:  spawn position on the X, Y and/or Z axes

- **Surface noise**:  the amount of normal-mapping applied to the surfaces of objects; increasing this parameter makes objects appear "rougher"

Each of these behaviours can be included or ignored from anomaly calculations according to the needs of the user.

# Anomaly Definition Model

Anomalies in the ANTShapes simulation are highlighted in red.  Objects that are non-anomalous appear blue (more common) to green (less common).

The properties listed above are defined per-object from zero-mean normal distributions, where standard deviation is between \[0, 1\].  Users define the mean and standard distribution values for each behaviour.  When the standard deviation = 0, the mean value acts as a constant value which is assigned to the property.

When an object is created, samples are taken from the normal distributions associated with each property.

The overall P-value is obtained by evaluating the cumulative distribution function over all behaviours included for anomaly definitions:

A pre-print publication discussing the anomaly definition model behind ANTShapes in further detail is available here: _coming soon lol_

# Tutorials, Usage, Documentation...

Please see the [project wiki](https://github.com/EDGYOrganism/ANTShapes/wiki).

# Builds

Compiled builds are available for Linux and Windows - please see the [releases page](https://github.com/EDGYOrganism/ANTShapes/releases).

No builds are planned for OSX unless demand is high enough!

# Credits

Mike Middleton [@itskobold](https://github.com/itskobold) - app development

EDGY Organism [@EDGYOrganism](https://github.com/EDGYOrganism) - master project, testing & feedback

[@hamsturcio](https://free3d.com/3d-model/teapot-15884.html) - High-poly Utah teapot-style 3D model

# Contact

For general inquiries - michael.middleton@york.ac.uk

**Please report bugs via the issues tab on the project GitHub!**
