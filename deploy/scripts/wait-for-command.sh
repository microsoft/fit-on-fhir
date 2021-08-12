#!/bin/bash
CMD=${1?}
TIMEOUT=${2:-"300"}

start=$SECONDS

timeout --foreground $TIMEOUT bash \
<<-EOD
  until [[ -n \$RET ]] && [[ \$RET -eq 0 ]]; do
    $CMD
    RET=\$? && echo -ne \$RET
    TRIES=\$(( TRIES + 1 )) && [[ \$(( TRIES % 10 )) == 0 ]] && echo
    sleep 1
  done
EOD

RET=$?
duration=$(( SECONDS - start ))

echo 

if [[ $RET -eq 0 ]]; then
    echo "$CMD was successful in $duration seconds"
else
    echo "$CMD timed out after $duration seconds waiting for success"
    exit 1
fi