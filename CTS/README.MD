# Central Tracking Server
## Setting up the Central Tracking Server

In order to set up the central tracking server, create a file called .env from the .env_sample file and fill it with the following information:
- SERVER_PORT: The port where the server will listen for requests
- 

## abbreviated API doc
- `GET /sensor` - search for every sensor, regardless of type.
- `GET /sensor?sensorid=A113.1` - search for the Sensor with the given name A113.1
- `GET /sensor?id=60ffd23aaec` - search for the Sensor with the mongoDB _id 60ffd23aaec
- `POST /sensor/pss/A113.1` - send passage event for the pss A113.1
- `POST /sensor/rss/A113.a` - send room observation for the rss A113.a
