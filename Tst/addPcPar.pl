use File::Find; 

sub wanted 
{
    if ($_ eq "testconfigPc.txt")
    {
        open(IN,"testconfigPc.txt") or die "Can't open testconfigPc.txt";
        open(OUT,">","testconfigPc.out") or die "Can't open testconfigPc.out"; 
    while (<IN>)
    {
       @lines = split(":");

       if ($lines[0] eq "acc")
       {
          print OUT "argPc: /outputDir:..\\.\n";
          print OUT "acc: .\\.\n";
       }
       else
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