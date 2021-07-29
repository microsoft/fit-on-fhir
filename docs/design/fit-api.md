# Data Migration Design

## Initial Migration vs Interval Sync

### Interval Sync

Every hour, each user's data from the previous hour will be migrated.  At the top of the hour, the Sync Event Generator will kick off, adding each user to the queue.  The Publish Data Function App will get triggered by the queue, and migrate data for the user tied to the queue message for the previous hour.

Example:

* 2:00pm - Sync Event Generator runs, adds users to queue
* 2:05pm - Publish Data Function App runs for user X, migrates data from 1-2pm

### Initial Migration

Some indicator may need to be stored in the user table and added to the queue message to tell the Publish Data Function App run for a particular user that this is the first run, and therefore needs to migrate the past 30 day's data.  This will start with the previous hour, and go backwards 30 days from there.

Example:

* 7/27 2:00pm - Sync Event Generator runs, adds users to queue (indicator on user X)
* 7/27 2:03pm - Publish Data Function App runs for user X, migrates data from 6/27 2pm-7/27 2pm.

## Google Fit API Breakdown

API Base: fitness.googleapis.com/fitness/v1/

* List DataSources: users/me/dataSources/
* Aggregate Datasource: users/me/dataset:aggregate

The below example will query all step data from Google Fit for a particular user, aggregated by hour buckets, between the two given timestamps.

Example POST Body:

```json
{
  "aggregateBy": [{
    "dataTypeName": "com.google.step_count.delta"
  }],
  "bucketByTime": { "durationMillis": 3600000 },
  "startTimeMillis": 1619269200000,
  "endTimeMillis": 1626969600000
}
```

* **3600000 ms = 1 hour**

Example Response:

```json
{
  "bucket": [
    {
      "startTimeMillis": "1619269200000", 
      "endTimeMillis": "1619272800000", 
      "dataset": [
        {
          "dataSourceId": "derived:com.google.step_count.delta:com.google.android.gms:aggregated", 
          "point": [
            {
              "startTimeNanos": "1619271999139323278", 
              "originDataSourceId": "raw:com.google.step_count.cumulative:Google:Pixel 5:22c38cf1a542006a:Step Counter", 
              "endTimeNanos": "1619272698394852945", 
              "value": [
                {
                  "mapVal": [], 
                  "intVal": 174
                }
              ], 
              "dataTypeName": "com.google.step_count.delta"
            }
          ]
        }
      ]
    }, 
    {
      "startTimeMillis": "1619272800000", 
      "endTimeMillis": "1619276400000", 
      "dataset": [
        {
          "dataSourceId": "derived:com.google.step_count.delta:com.google.android.gms:aggregated", 
          "point": [
            {
              "startTimeNanos": "1619272929352595425", 
              "originDataSourceId": "raw:com.google.step_count.cumulative:Google:Pixel 5:22c38cf1a542006a:Step Counter", 
              "endTimeNanos": "1619273088723834289", 
              "value": [
                {
                  "mapVal": [], 
                  "intVal": 94
                }
              ], 
              "dataTypeName": "com.google.step_count.delta"
            }
          ]
        }
      ]
    },...
```

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

`users/me/dataSources/derived:com.google.active_minutes:com.google.android.gms:merge_active_minutes/datasets/0-1627490142702000000`
```json
{
  "minStartTimeNs": "0", 
  "maxEndTimeNs": "1627490142702000000", 
  "dataSourceId": "derived:com.google.active_minutes:com.google.android.gms:merge_active_minutes", 
  "point": [
    {
      "modifiedTimeMillis": "1575335024555", 
      "startTimeNanos": "1575331980000000000", 
      "endTimeNanos": "1575332040000000000", 
      "value": [
        {
          "mapVal": [], 
          "intVal": 1
        }
      ], 
      "dataTypeName": "com.google.active_minutes", 
      "originDataSourceId": "derived:com.google.step_count.delta:com.google.android.gms:estimated_steps"
    }, 
    {
      "modifiedTimeMillis": "1575335024555", 
      "startTimeNanos": "1575332040000000000", 
      "endTimeNanos": "1575332100000000000", 
      "value": [
        {
          "mapVal": [], 
          "intVal": 1
        }
      ], 
      "dataTypeName": "com.google.active_minutes", 
      "originDataSourceId": "derived:com.google.step_count.delta:com.google.android.gms:estimated_steps"
    }, 
```

users/me/dataSources/derived:com.google.heart_rate.bpm:com.google.android.gms:merge_heart_rate_bpm/datasets/0-1627490142702000000

```json
{
  "minStartTimeNs": "0", 
  "maxEndTimeNs": "1627490142702000000", 
  "dataSourceId": "derived:com.google.heart_rate.bpm:com.google.android.gms:merge_heart_rate_bpm", 
  "point": [
    {
      "modifiedTimeMillis": "1615568861420", 
      "startTimeNanos": "1615565625294000000", 
      "endTimeNanos": "1615565625294000000", 
      "value": [
        {
          "mapVal": [], 
          "fpVal": 85.86285400390625
        }
      ], 
      "dataTypeName": "com.google.heart_rate.bpm", 
      "originDataSourceId": "raw:com.google.heart_rate.bpm:com.google.android.apps.fitness:user_input"
    }, 
    {
      "modifiedTimeMillis": "1615584621143", 
      "startTimeNanos": "1615584287536000000", 
      "endTimeNanos": "1615584287536000000", 
      "value": [
        {
          "mapVal": [], 
          "fpVal": 76.31412506103516
        }
      ], 
      "dataTypeName": "com.google.heart_rate.bpm", 
      "originDataSourceId": "raw:com.google.heart_rate.bpm:com.google.android.apps.fitness:user_input"
    }, 
    {
      "modifiedTimeMillis": "1615591528319", 
      "startTimeNanos": "1615585689211000000", 
      "endTimeNanos": "1615585689211000000", 
      "value": [
        {
          "mapVal": [], 
          "fpVal": 83.35336303710938
        }
      ], 
      "dataTypeName": "com.google.heart_rate.bpm", 
      "originDataSourceId": "raw:com.google.heart_rate.bpm:com.google.android.apps.fitness:user_input"
    }, 
    {
      "modifiedTimeMillis": "1615777445180", 
      "startTimeNanos": "1615775930059000000", 
      "endTimeNanos": "1615775930059000000", 
      "value": [
        {
          "mapVal": [], 
          "fpVal": 70.96192169189453
        }
      ], 
      "dataTypeName": "com.google.heart_rate.bpm", 
      "originDataSourceId": "raw:com.google.heart_rate.bpm:com.google.android.apps.fitness:user_input"
    }
  ]
}
```

Heart Rate Aggregate
```json
{
  "aggregateBy": [{
    "dataTypeName": "com.google.heart_rate.bpm"
  }],
  "bucketByTime": { "durationMillis": 3600000 },
  "startTimeMillis": 1615585689210,
  "endTimeMillis": 1615585689211
}
```

```json
{
  "bucket": [
    {
      "startTimeMillis": "1615585689210", 
      "endTimeMillis": "1615585689211", 
      "dataset": [
        {
          "dataSourceId": "derived:com.google.heart_rate.summary:com.google.android.gms:aggregated", 
          "point": []
        }
      ]
    }
  ]
}
```

https://fitness.googleapis.com/fitness/v1/users/me/dataSources/derived:com.google.heart_rate.summary:com.google.android.gms