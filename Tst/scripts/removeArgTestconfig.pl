#removes lines ending with "dll" from testconfig.txt

use File::Find; 

sub wanted 
{
    if ($_ eq "testconfig.txt")
    {
        open(IN,"testconfig.txt") or die "Can't open testconfig.txt";
        open(OUT,">","testconfig.out") or die "Can't open testconfig.out"; 
    while (<IN>)
    {
       @lines = split(" ");
	   $var = substr($lines[1], -3);
	   if (not ($var eq "dll"))
       {
          print OUT $_; 
       }
    }

    close IN;
    close OUT;
    system("move testconfig.out testconfig.txt"); 
   }    

}


find(\&wanted,$ARGV[0]); 