# TinFluxWS
The Tinkerforge InfluxDB Weather Station. Short: TinFluxWS

## What for
This tiny docker image will collect periodically [Tinkerforge](https://www.tinkerforge.com/) sensor values and saves them into a InfluxDB.

As a nice feature, since version 1.7, TinFluxWS calculates things like:
* Saturation vapor pressure
* Water vapor partial pressure
* Specific humidity
* Max specific humidity
* Mixing ratio - moisture level
* Dense water vapor
* Dense dry air
* Dense humid air
* Dew point temperature
* Absolute humidity
* Altitude (with offset possibility - see TINFLUXWS_ALTITUDEOFFSET env. var.)

__Please keep in mind that these calculations require a temperature, humidity and air pressure Bricklet!__ 

### Graphing

Thanks to the amazing [Grafana project](https://grafana.com/) you can graph then the measured sensor values.

## Currently supported Tinkerforge Bricklets
* [Temperature Bricklet](https://www.tinkerforge.com/en/doc/Hardware/Bricklets/Temperature.html)
* [Humidity Bricklet V1](https://www.tinkerforge.com/en/doc/Hardware/Bricklets/Humidity.html)
* [Barometer Bricklet](https://www.tinkerforge.com/en/doc/Hardware/Bricklets/Barometer.html)

These sensors will automatically be detected.


## Supported tags and respective ```Dockerfile``` links

-	[`1.7`, `latest` (*TinFluxWS/1.7/Dockerfile*)](https://github.com/akeusen/tinfluxws/blob/1.7/Dockerfile)
-	[`1.6` (*TinFluxWS/1.6/Dockerfile*)](https://github.com/akeusen/tinfluxws/blob/1.6/Dockerfile)
-	[`1.5` (*TinFluxWS/1.5/Dockerfile*)](https://github.com/akeusen/tinfluxws/blob/1.5/Dockerfile)



# Using this Image

## Requirements
* Tinkerforge [Master Brick](https://www.tinkerforge.com/en/doc/Hardware/Bricks/Master_Brick.html) to connect the Bricklets.
* A InfluxDB database ([create a new or take a existing](https://docs.influxdata.com/influxdb/v1.5/query_language/database_management/#create-database)) 
  * There is a [docker image](https://hub.docker.com/_/influxdb/) available.
* Influx user with write and read permission on the created/used database [Link to Influx-Docs](https://docs.influxdata.com/influxdb/v1.5/query_language/authentication_and_authorization/#user-management-commands)
* A Grafana instance for graphing the data. 
  * There is also a [docker image](https://hub.docker.com/r/grafana/grafana/) available!

## Configuration
The TinFluxWS image uses several environment variables to automatically configure certain parts of the application. They may significantly aid you in using this image.

##### TINFLUXWS_STATIONNAME

Adds a name to the TinFluxWStaion. This name will also be the measurement name within the provided InfluxDB database. 

##### TINFLUXWS_MASTERBRICK_HOST

Lets the application know on which IP/Hostname the Master Brick can be found.  

##### TINFLUXWS_MASTERBRICK_PORT

The TCP port on which the Master Brick will listen. 

##### TINFLUXWS_CALLBACKPERIOD

Sets the frequent (as integer in seconds) for measuring the environment.

##### TINFLUXWS_ALTITUDEOFFSET

Sets the offset (can be a negative or positive integer) for the altitude calculation.
This may be necessary because the calculation works with approximations constants.

##### TINFLUXWS_INFLUXDB_HOST_URI

The URI to connect to the Influx RESTful API

##### TINFLUXWS_INFLUXDB_NAME

The Influx database name. In this DB will measurements be placed. 

##### TINFLUXWS_INFLUXDB_USER

Influx user with write permissions

__Consider using env-files or docker secrets for storing this sensitive data!__ 

##### TINFLUXWS_INFLUXDB_PASSWD

Influx user's password

__Consider using env-files or docker secrets for storing this sensitive data!__


# Docker-Compose example file

```YAML
version: "3.5"

services:
  tinfluxweatherstation:
    image: akeusen/tinfluxws:latest
    container_name: tinfluxws
    restart: always
    environment:
      - TINFLUXWS_STATIONNAME=myWeatherStationName
      - TINFLUXWS_MASTERBRICK_HOST=masterb.domain.local
      - TINFLUXWS_MASTERBRICK_PORT=4223
      - TINFLUXWS_CALLBACKPERIOD=300
      - TINFLUXWS_ALTITUDEOFFSET=29
      - TINFLUXWS_INFLUXDB_HOST_URI=https://influxhost.domain.local:443
      - TINFLUXWS_INFLUXDB_NAME=myInfluxDB
      
    # Because of security reasons, the following env variables should be placed in a separate .env file.
    # Even better use Docker secrets: https://docs.docker.com/engine/swarm/secrets/
    # - TINFLUXWS_INFLUXDB_USER=
    # - TINFLUXWS_INFLUXDB_PASSWD=
    
  # env_file:
    # - ./docker.env
    
    secrets:
      - influxuser
      - influxpassword

# File based docker secrets (if not in swarm mode):
secrets:
  influxuser:
    file: ./dbusername
  influxpassword:
    file: ./dbpassword
```

# License

View [license information](https://github.com/akeusen/tinfluxws/blob/master/LICENSE) for the software contained in this image.

As with all Docker images, these likely also contain other software which may be under other licenses (such as Bash, etc from the base distribution, along with any direct or indirect dependencies of the primary software being contained).

As for any pre-built image usage, it is the image user's responsibility to ensure that any use of this image complies with any relevant licenses for all software contained within.


# Versions
* __05.07.2018__: Add altitude calculation and some code refactoring (1.7)
* __02.07.2018__: New air calculation features like absolute humidity added (1.6)
* __30.06.2018__: Initial public release (1.5)

