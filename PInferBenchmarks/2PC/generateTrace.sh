n=$1
ef="${@:2}"

for tc in tcC2P3T3 tcC3P4T3 tcC3P5T3 tcSingleClientNoFailure
do
    if ((${#ef} == 0))
    then
        echo "Run $tc with $n schedules for all events"
        p check -tc $tc -s $n --pinfer -tf /scratch/gpfs/dh7120/$(($n*4))
    else
        echo "Run $tc with $n schedules with events $ef"
        p check -tc $tc -s $n -ef $ef --pinfer -tf /scratch/gpfs/dh7120/$(($n*4))
    fi
done
echo "Finished"
