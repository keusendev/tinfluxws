# TinFluxWS
The Tinkerforge InfluxDB Weather Station. Short: TinFluxWS

## What for
This tiny docker image will collect periodically [Tinkerforge](https://www.tinkerforge.com/) sensor values and saves them into a InfluxDB.
Thanks to the amazing [Grafana project](https://grafana.com/) you can graph then the measured sensor values.

## Currently supported Tinkerforge Bricklets
* [Temperature Bricklet](https://www.tinkerforge.com/en/doc/Hardware/Bricklets/Temperature.html)
* [Humidity Bricklet V1](https://www.tinkerforge.com/en/doc/Hardware/Bricklets/Humidity.html)
* [Barometer Bricklet](https://www.tinkerforge.com/en/doc/Hardware/Bricklets/Barometer.html)

The sensors will automatically be detected.


## Supported tags and respective ```Dockerfile``` links

-	[`1.5`, `latest` (*TinFluxWS/1.5/Dockerfile*)](https://github.com/akeusen/tinfluxws/blob/1.5/Dockerfile)



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

##### TRIFLUXWS_STATIONNAME

Adds a name to the TinFluxWStaion. This name will also be the measurement name within the privided InfluxDB database. 

##### TRIFLUXWS_MASTERBRICK_HOST

Lets the application know on whicht IP/Hostname the Master Brick can be found.  

##### TRIFLUXWS_MASTERBRICK_PORT

The TCP port on which the Master Brick will listen. 

##### TRIFLUXWS_INFLUXDB_HOST_URI

The URI to connect to the Influx RESTful API

##### TRIFLUXWS_INFLUXDB_NAME

The Influx database name. In this DB will measurements be placed. 

##### TRIFLUXWS_INFLUXDB_USER

Influx user with write permissions

__Consoder using env-files or docker secrets for storing this sensitiv data!__ 

##### TRIFLUXWS_INFLUXDB_PASSWD

Influx user's password

__Consoder using env-files or docker secrets for storing this sensitiv data!__


# Docker-Compose example file

```YAML
version: "3.5"

services:
  tinfluxweatherstation:
    image: akeusen/tinfluxws:latest
    container_name: tinfluxws
    restart: always
    environment:
      - TRIFLUXWS_STATIONNAME=myWeatherStationName
      - TRIFLUXWS_MASTERBRICK_HOST=masterb.domain.local
      - TRIFLUXWS_MASTERBRICK_PORT=4223
      - TRIFLUXWS_INFLUXDB_HOST_URI=https://influxhost.domain.local:443
      - TRIFLUXWS_INFLUXDB_NAME=myInfluxDB
      
    # Because of security reasons, the following env variables should be placed in a separate .env file.
    # Even better use Docker secrets: https://docs.docker.com/engine/swarm/secrets/
    # - TRIFLUXWS_INFLUXDB_USER=
    # - TRIFLUXWS_INFLUXDB_PASSWD=
    
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
* __30.06.2018__: Initial public release (1.5)

