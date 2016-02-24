use File::Find; 

sub wanted 
{
    if ($_ eq "testconfigPc.txt")
    {
        open(IN,"testconfigPc.txt") or die "Can't open testconfigPc.txt";
        open(OUT,">","testconfigPc.out") or die "Can't open testconfigPc.out"; 
    while (<IN>)
    {
       @lines = split(": ");
	   if ($lines[0] eq "argPc")
       {
          print OUT "arg: " . $lines[1]; 
       }
	   if ($lines[0] eq "incPc")
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
    system("move testconfigPc.out testconfigPc.txt"); 
   }    

}


find(\&wanted,$ARGV[0]); 