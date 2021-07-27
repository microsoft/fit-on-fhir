# Initial Migration vs Interval Sync

## Interval Sync

Every hour, each user's data from the previous hour will be migrated.  At the top of the hour, the Sync Event Generator will kick off, adding each user to the queue.  The Publish Data Function App will get triggered by the queue, and migrate data for the user tied to the queue message for the previous hour.

Example:

* 2:00pm - Sync Event Generator runs, adds users to queue
* 2:05pm - Publish Data Function App runs for user X, migrates data from 1-2pm

## Initial Migration

Some indicator may need to be stored in the user table and added to the queue message to tell the Publish Data Function App run for a particular user that this is the first run, and therefore needs to migrate the past 30 day's data.  This will start with the previous hour, and go backwards 30 days from there.

Example:

* 7/27 2:00pm - Sync Event Generator runs, adds users to queue (indicator on user X)
* 7/27 2:03pm - Publish Data Function App runs for user X, migrates data from 6/27 2pm-7/27 2pm.

# Google Fit API Breakdown
* List DataSources: https://fitness.googleapis.com/fitness/v1/users/me/dataSources/
* Aggregate Datasource: https://fitness.googleapis.com/fitness/v1/users/me/dataset:aggregate

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