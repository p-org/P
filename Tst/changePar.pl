use File::Find; 

sub wanted 
{
    if ($_ eq "testconfigPrt.txt")
    {
        open(IN,"testconfigPrt.txt") or die "Can't open testconfigPrt.txt";
        open(OUT,">","testconfigPrt.out") or die "Can't open testconfigPrt.out"; 
    while (<IN>)
    {
       @lines = split(":");

       if ($lines[0] eq "runPrt")
       {
          print OUT "runPrt: ..\\..\\..\\..\\..\\..\\Tst\\PrtTester\\Debug\\Tester.exe\n";
       }
       else
       {
          print OUT $_; 
       }
    }

    close IN;
    close OUT;
    system("move testconfigPrt.out testconfigPrt.txt"); 
   }    

}


find(\&wanted,$ARGV[0]); 