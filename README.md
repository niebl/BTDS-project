# BTDS-project
Room Monitoring
Authors:
Maximilian Busch
Felix Niebl

## Goal
Implementation of a System that can create an internal model of a building complex at room scale resolution and track the amount of people in each room within reasonable accuracy. Additionally, the System to be developed needs to be able to track the distance of inhabitants to each other at certain areas of interests and ring an alarm in case a minimum safety distance is not met.

To reach this Goal we created a System containing 3 components: 
1. Room surveillance sensor  
A sensor system that consistently observes an area of interest in a given room and sends the positions of it's inhabitants to the central server.

3. Passageway Surveillance sensor  
A sensor system that consistently observes a passageway or an entrance to a room and notifies the server whenever a person enters or leaves a room.

5. Central Tracking Server  
The Central server, which is tasked with keeping track of the inhabitant numbers of each room by using information from an arbitrary amount of both types of sensor systems.

## installation requirements

## installation instructions

## use instructions

## example of use

## disclaimer
At this point in time, this system is not reliable enough for any use such as social distancing enforcement. We are not liable for any damages that may occur in actual use.
