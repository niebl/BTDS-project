# BTDS-project
Room Monitoring
Authors:
Maximilian Busch
Felix Niebl

## Goal
Implementation of a System that can create an internal model of a building complex at room scale resolution and track the amount of people in each room within reasonable accuracy. Additionally, the System to be developed needs to be able to track the distance of inhabitants to each other at certain areas of interests and ring an alarm in case a minimum safety distance is not met.

To reach this Goal we created a System containing 3 components: 
1. Room surveillance sensor (RSS)  
A sensor system that consistently observes an area of interest in a given room and sends the positions of it's inhabitants to the central server.

3. Passageway Surveillance sensor (PSS)  
A sensor system that consistently observes a passageway or an entrance to a room and notifies the server whenever a person enters or leaves a room.

5. Central Tracking Server (CTS)  
The Central server, which is tasked with keeping track of the inhabitant numbers of each room by using information from an arbitrary amount of both types of sensor systems.

## installation requirements
Each instance of an RSS and PSS will require a kinect V2 sensor and a separate windows machine that is capable of supporting such Sensors.
The CTS can run on any system that is capable of supporting the used components: nodeJS and mongoDB. Generally it is possible to run the CTS on one of the sensor-machines.
All machines used need to be able to communicate with the CTS-host-machine via a network connection.

## installation instructions
### CTS installation
Installation
1. Install the nodeJS runtime environment on your computer: https://nodejs.org/en/
2. Install the mongoDB community server on the same computer: https://www.mongodb.com/try/download/community (it may also be possible to use a server on a different machine or in a cloud. It will however need a different address)
3. Once ensured that these systems are running, clone the project repository https://github.com/niebl/BTDS-project 
4. Use a CLI of your choice to navigate to the CTS directory and run “> npm install” (if this doesn’t work, check if node is added to the path variables)
5. Make a copy of the file “.env_sample” and rename it to “.env”. Open it in an editor and change the variables if you need to.
6. Start the server using “npm start”.
7. Open your browser and visit “localhost:3000”, to see if the server is working (or any other port if you have changed the .env).
8. You may also view the page on a mobile device within the same network by visiting “{server-IP-address}:3000” in the browser of the device. This can be useful when testing the system.

If the page doesn’t load and the console returns “UnhandledPromiseRejectionWarning: MongooseError: Operation”, make sure the mongoDB server is running and accessible over the address that was specified in the .env.

### installation PSS and RSS
(also referred to as Sensor-System)
1. On each sensor-system, configure and install the Kinect V2 depth-Sensor. For this, refer to this guide: https://github.com/violetasdev/bodytrackingdepth_course/wiki/Kinect-V2.
2. To have a tool to build the sensor-system-applications from the repository, install and configure Visual Studio 2019 for the kinect V2. For this, refer to this guide: https://github.com/violetasdev/bodytrackingdepth_course/blob/master/KinectV2/docs/visualStudio2019_doc.md.
3. Clone the project repository and navigate to the directory “PassagewaySensorSystem” or “RoomSensorSystem” respectively, depending on which type of sensor-system you intend to set up.
4. Make sure the Kinect V2 sensor is plugged in and powered using the Kinect configuration verifier.
5. Build and run the application.
6. If the window displays the Sensor view or a top-down view in which bodies are tracked, the application is correctly receiving data from the sensor.



## use instructions

## example of use

## disclaimer
At this point in time, this system is not reliable enough for any use such as social distancing enforcement. We are not liable for any damages that may occur in actual use. Before implementing such surveillance systems also consider that privacy is an essential human right that should not be violated.
