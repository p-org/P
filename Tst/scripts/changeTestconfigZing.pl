use File::Find; 

sub wanted 
{
    if ($_ eq "testconfigZing.txt")
    {
        open(IN,"testconfigZing.txt") or die "Can't open testconfigZing.txt";
        open(OUT,">","testconfigZing.out") or die "Can't open testconfigZing.out"; 
    while (<IN>)
    {
       @lines = split(": ");
	   if ($lines[0] eq "argZing")
       {
          print OUT "arg: " . $lines[1]; 
       }
	   if ($lines[0] eq "incZing")
       {
          print OUT "inc: " . $lines[1]; 
       }
	   if ($lines[0] eq "del")
       {
          print OUT $_; 
       }
       if ($lines[0] eq "dsc")
       {
          print OUT $_; 
       }
    }

    close IN;
    close OUT;
    system("move testconfigZing.out testconfigZing.txt"); 
   }    

}


find(\&wanted,$ARGV[0]); 