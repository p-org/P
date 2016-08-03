use File::Find; 

sub wanted 
{
    #print "next file:\n";
	#print $_;
	#print "\n";
	@temp = substr($_, length($_)-2, 2);
	#print @temp;
	#print "\n";
    if (substr($_, length($_)-2, 2) eq ".p")
    {
		print "found .p file:\n";
		print $_;
		print "\n";
		@fileName = $_;
		#print "before open\n";
        open(IN, $_) or die "Can't open .p";
		#print "after open\n";
        open(OUT,">","temp.out") or die "Can't open temp.out"; 
		#print "after 2nd open\n";
    while (<IN>)
    {
       @lines = split(" ");

       if ($lines[0] eq "main")
       {
          print OUT "machine Main {\n";
       }
       else
       {
          print OUT $_; 
       }
    }
	#print "after changes are done\n";
    close IN;
    close OUT;
	system("move temp.out @fileName");
   }    

}


find(\&wanted,$ARGV[0]); 