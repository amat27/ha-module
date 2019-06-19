# Anticorruption HA Module [![Build status](https://ci.appveyor.com/api/projects/status/37ngnmo4eibw42rg/branch/develop?svg=true)](https://ci.appveyor.com/project/amat27/ha-module/branch/develop) [![codecov](https://codecov.io/gh/amat27/ha-module/branch/develop/graph/badge.svg)](https://codecov.io/gh/amat27/ha-module)
Algorithms for doing leader election and name resolving with the help of another HA system, serves as anticorruption layer.

## Specification

### Parameters
 - `I`: intervalfor heartbeat (e.g. 1 sec)
 - `T`: heartbeat timeout (e.g. 5 secs)
 - `T > 2 * I`

### Data
 - `Heartbeat Table`: A table in the external HA system contains heartbeat entry.
 - `Heartbeat Entry`: in the format `{uuid, utype, timestamp}`
 - `ha_time`: current date time of the external HA system
 - All time is in UTC time

### Procedures
 - `UpdateHeartBeat(uuid, utype)`:
   
   For each type, update entry `{old_uuid, utype, old_timestamp}` in heartbeat table with `{uuid, utype, ha_time}`.

   For each type, if `uuid` is not equal to `old_uuid`, then (`ha_time – old_timestamp > T`) must be satisfied.

   The update process uses optimistic concurrency control. e.g. if the heartbeat entry has been updated before another heartbeat reaches, the later heartbeat is discarded.

 - `GetPrimary(utype)`:

   Return `(uuid, utype)` in heartbeat entry with the corresponding query utype if (`ha_time - timestamp <= T`). Else return empty value.

### Algorithm
1. After a client S started, it generates a unique instance ID `uuid` to identify itself and marks itself with the exact `utype`, which it will work as in the future.

2. S calls `GetPrimary(utype)` every `I` secs.

3. If `GetPrimary(utype)` returned empty value, S calls `UpdateHeartbeat(uuid, utype)`.

4. Continue to call `GetPrimary(utype)` every `I` secs.

    a. If subsequent call to `GetPrimary(utype)` returns `(uuid, utype)` generated in 1, S will then work as primary.

    b. If subsequent call to `GetPrimary(utype)` returns a unique ID which is different from `uuid` and the same type with `utype` generated in 1, go back to 2.

    c. If subsequent call to `GetPrimary(utype)` returns an empty value / a corrupted message, error occurred in 3. Retry 3.

5. S call `UpdateHeartBeat(uuid, utype)` and `GetPrimary(utype)` every `I` sec.

    a. If `GetPrimary(utype)` returns anything except `(uuid, utype)`, or didn't return for `(T - I)` secs, exit itself and restart.



## Spec in TLA+

[Source Code](hpcha.tla)
