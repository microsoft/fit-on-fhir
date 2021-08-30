# Data Migration Design

## Initial Migration vs Interval Sync

### Interval Sync

Every hour, each user's data since the last migrated timestamp for that user will be migrated.  At the top of the hour, the Sync Event Generator will kick off, adding each user to the queue.  The Publish Data Function App will get triggered by the queue, and migrate data for the user.

Example:

* 2:00pm - Sync Event Generator runs, adds users to queue
* 2:05pm - Publish Data Function App runs for user X, migrates data from (_last timestamp_)-(_now_)
         - Update users table last migrated timestamp to be (_now_)

### Initial Migration

An initial migration will be done when the user's last migrated timestamp is not set in the user's table.  In this case, the last 30 days of data will be migrated for the user.

Example:

* 7/27 2:00pm - Sync Event Generator runs, adds users to queue
* 7/27 2:03pm - Publish Data Function App runs for user X, migrates data from 6/27 (_current time_)-7/27 (_current time_).

## Google Fit API Breakdown

API Base: fitness.googleapis.com/fitness/v1/

* List DataSources: users/me/dataSources/
* Datasource dataset: users/me/dataSources/{{datasourceId}}/datasets/{{datasetId}}

Data Types:

| Data   Type       | dataTypeName                 |
|-------------------|------------------------------|
| Heart Rate        | com.google.heart_rate.bpm    |
| Step Count        | com.google.step_count.delta  |
| Oxygen Saturation | com.google.oxygen_saturation |
| Body Temperature  | com.google.body.temperature  |
| Respiratory Rate  | ?                            |
| Blood Pressure    | com.google.blood_pressure    |
| Blood Glucose     | com.google.blood_glucose     |
| Body Mass         | com.google.weight            |
| Height            | com.google.height            |

We will be sending individual data points to the IoMT connector with no aggregation.  This means that we will first need to query the datasources available for each user to find the particular dataType we are interested in (for example, com.google.blood_glucose), and then prepare to query the individual datasets for each of those datasources.

The dataset endpoint takes a datasourceId and a datasetId - the datasetId is crafted as a representation of a range of two unix epoch timestamps in nanoseconds, for example: 1574159699023000000-1574159699023000000

The below example will query a dataset from a particular blood glucose data source:

/users/me/dataSources/raw:com.google.blood_glucose:407408718192:BloodGlucose/datasets/1574159699023000000-1574159699023000000

Example Response:

```json
{
    "minStartTimeNs": "1574159699023000000",
    "maxEndTimeNs": "1574159699023000000",
    "dataSourceId": "raw:com.google.blood_glucose:407408718192:BloodGlucose",
    "point": [
        {
            "startTimeNanos": "1574159699023000000",
            "endTimeNanos": "1574159699023000000",
            "dataTypeName": "com.google.blood_glucose",
            "value": [
                {
                    "fpVal": 5,
                    "mapVal": []
                },
                {
                    "intVal": 3,
                    "mapVal": []
                },
                {
                    "intVal": 1,
                    "mapVal": []
                },
                {
                    "intVal": 3,
                    "mapVal": []
                },
                {
                    "intVal": 2,
                    "mapVal": []
                }
            ],
            "modifiedTimeMillis": "1627675845579"
        }
    ]
}
```
