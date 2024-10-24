n=$1
ef="${@:2}"

for tc in tcC1R3 tcC2R3 tcC3R3 tcC4R3 tcC3R5
do
    if ((${#ef} == 0))
    then
        echo "Run $tc with $n schedules for all events"
        p check -tc $tc -s $n --pinfer -tf /scratch/gpfs/dh7120/clockbound/$(($n*5))
    else
        echo "Run $tc with $n schedules with events $ef"
        p check -tc $tc -s $n -ef $ef --pinfer -tf /scratch/gpfs/dh7120/clockbound/$(($n*5))
    fi
done
echo "Finished"