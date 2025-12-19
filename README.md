# Where's My Stalker

Android only, though scene mode also works as some additional dummy BLE devices are mocked for testing.

The basic idea is just slapping on BLE advertisments received onto AR locations and creating a heatmap that way with the RSSI's.

You can filter the heatmap based on the MAC / company. There's some additional tagging for Apple devices.

Android bluetooth is a bit of a pain to work with so connections and service queries to assigned numbers (such as the name / info service, see https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Assigned_Numbers/out/en/Assigned_Numbers.pdf ) aren't supported. 

As mentioned before, just the BLE advertisements are being used here.

The app locally collects and logs:
- Device MAC addresses and names
- Signal strength (RSSI) measurements
- Transmit power (TX Power)
- Device connectability status
- Local position coordinates
- GPS coordinates (latitude/longitude)
- Timestamps for all measurements


# Rendering
Currently the Unity URP stack is being used, in addition to some post processing effects on the heatmap to make things look more pallatable.
Note that render textures have some issues with being displayed on Android for some reason, though the current code works for a Google Pixel 7.

The heatmap is just an ortho camera, which hovers over the player and only renders the datapoints to a rendertexture. 
The main camera (the actual AR <-> scene mapping) is also renderred into a rendertexture, which is then placed onto the UI. 

Just as a heads up, the render textures are made dynamically because they need to be addressable.


# Usage

<img width="1080" height="2400" alt="Screenshot_20251218-232328" src="https://github.com/user-attachments/assets/09397200-bb96-4c65-9213-03618449440f" />
