*******************
** Documentation **
*******************
Please visit: http://saladgamer.com/vlb-doc/

*******************
** Sample Scenes **
*******************
- demoScene: showcase multiple features
- demoStressTest: features 400 dynamic Volumetric Spotlights


*******************
** Configuration **
*******************
In your project file, look for a file named Config.asset under the folder Plugins/VolumetricLightBeam/Resources. In the inspector, you can configure the following properties:
- Beam Geometry:
  - Override Layer: controls on which layer the beam geometry meshes will be created in.
  - Tag: the tag applied on the procedural geometry GameObjects.

- Rendering:
  - Render Queue: Determine in which order beams are rendered compared to other objects. This way for example transparent objects are rendered after opaque objects, and so on.
  - Render Pipeline: This property is automatically set to the proper value depending on the render pipeline used in your project, but you can change it.
  - Rendering Mode: Multi-Pass, Single-Pass, GPU Instancing or SRP Batcher.
  - Dithering: Depending on the quality of your screen, you might see some artifacts with high contrast visual (like a white beam over a black background). These is a very common problem known as color banding. To help with this issue, the plugin offers a Dithering factor: it smooths the banding by introducing a subtle pattern of noise.

- Shared Mesh:
  - Sides: Number of Sides of the cone (tessellation). Higher values make the beam looks more "round", but at performance cost.
  - Segments: Number of Segments of the cone. Higher values give better looking results, but at performance cost. 

- Global 3D Noise:
  - Scale: Global 3D Noise texture scaling. Higher scale make the noise more visible, but potentially less realistic.
  - Velocity: Global World Space direction and speed of the noise scrolling, simulating the fog/smoke movement.

- Camera to compute Fade Out
  - Fade Out Camera Tag: Tag used to retrieve the camera used to compute the fade out factor on beams.


****************************************
** Volumetric Light Beam - Properties **
****************************************
Basic:
- Color: Use the combobox to specify if you want to apply a Flat/Plan or a Gradient color.
- Color Flat: Use the color picker to set the color of the beam (takes account of the alpha value).
- Color Gradient: Apply a gradient along the light beam. The color and alpha variations will be applied.
- Blending Mode: Change how the light beam colors will be mixed with the scene.
- Intensity: Global beam intensity. If you want to control values for inside and outside independently, use the advanced mode.

- Spot Angle: Define the angle (in degrees) at the base of the beam's cone.
- Side Thickness: Thickness of the beam when looking at it from the side.

- Glare Frontal: Boost intensity factor when looking at the beam from the inside directly at the source.
- Glare from Behind: Boost intensity factor when looking at the beam from behind.

- Track Changes During Playtime: If true, the light beam will keep track of the changes of its own properties and the spotlight attached to it (if any) during playtime. This would allow you to modify the light beam in realtime from Script, Animator and/or Timeline. Enabling this feature is at very minor performance cost. So keep it disabled if you don't plan to modify this light beam during playtime.

Attenuation:
- Equation: Attenuation equation used to compute fading between Fade Start Distance and Range Distance.
- Range Distance: Maximum distance (in units) of the light beam. After this distance, the beam is entirely faded out.
- Fade Start Distance: Distance from the light source (in units) the beam intensity will start to fall off.

3D Noise:
- Enabled: Enable or disable the 3D Noise effect and select the mode. The mode you select will change how the noise will look when the beam is moved in realtime.
- Intensity: Higher intensity means the noise contribution is stronger and more visible.
- Scale: 3D Noise texture scaling. Higher scale make the noise more visible, but potentially less realistic.
- Velocity: World Space direction and speed of the noise scrolling, simulating the fog/smoke movement.

Soft Intersections Blending Distances:
- Camera: Distance from the camera the beam will fade. 0 = hard intersection. Higher values produce soft intersection when the camera is near the cone triangles.
- Opaque Geometry: Distance from the world geometry the beam will fade. 0 = hard intersection Higher values produce soft intersection when the beam intersects other opaque geometry.

Cone Geometry:
- Truncated Radius: Radius (in units) at the beam's source (the top of the cone). 0 will generate perfect cone geometry. Higher values will generate truncated cones.
- Cap Geom: Show the cap of the cone or not (only visible from the inside).
- Mesh Type:
  * Shared: Use the global shared mesh (recommended setting, since it will save a lot on memory). Will use the geometry properties set on global config.
  * Custom: Use a custom mesh instead. Will use the geometry properties set on the beam. 
- Custom Sides: Number of Sides of the cone (tessellation). Higher values make the beam looks more "round". The higher the value, the more memory and performance is required.
- Custom Segments: Number of Segments of the cone. Higher values give better looking results but more memory and performance would be required.

Fade Out:
- Enabled: Enable the Fade Out feature.
- Begin Distance: Fade out starting distance. Beyond this distance, the beam intensity will start to be dimmed.
- End Distance: Fade out ending distance. Beyond this distance, the beam will be culled off to save on performance.

2D:
- Dimensions:
 * 3D: the beam is generated along the Z axis.
 * 2D: the beam is generated along the X axis, so you won't have to rotate it to see it in 2D.
- Sorting Layer: The layer used to define this beam's overlay priority during rendering with 2D Sprites. This works the same way than the Sorting Layer* property of the Sprite Renderer class.
- Order in Layer: The overlay priority within its layer. Lower numbers are rendered first and subsequent numbers overlay those below. This works the same way than the Order in Layer property of the Sprite Renderer class.
