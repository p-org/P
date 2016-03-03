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
	   if (not ($lines[1] eq "/printTypeInference"))
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