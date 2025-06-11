# ANTShapes [Anomalous Neuromorphic Tool for Shapes]

_This project is currently in an open alpha state of development.  There are some missing and incomplete features for CPU-based rendering, and optimisation is needed.  Please feel free to try the software anyway and suggest features :)_

_Example datasets ~~are~~ will be available if you'd rather not download the executable._

ANTShapes is a neuromorphic vision dataset simulator for anomaly detection.  It simulates scenes populated by 3D objects and renders event data in a similar manner to real-life Dynamic Vision Sensors (DVSs).

The tool was designed to address the lack of high-quality fully-labelled datasets for the training of Spiking Neural Networks (SNNs) for anomaly detection in computer vision.

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

# Paper

A pre-print publication discussing the anomaly definition system behind ANTShapes is available here: _coming soon lol_

# Example Datasets

Also coming soon...

# Credits

Mike Middleton @itskobold - app development

Edgy Organism @EDGYOrganism - testing & feedback

@hamsturcio - High-poly Utah teapot-style 3D model (https://free3d.com/3d-model/teapot-15884.html)

# Contact

For general inquiries - michael.middleton@york.ac.uk

**Please report bugs on GitHub!**
