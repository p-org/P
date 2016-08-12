# This script doesn't work - see line 54
use File::Find; 

sub trim($);
sub rtrim($);

sub wanted 
{
	#print "next file:\n";
	#print $_;
	#@temp = substr($_, length($_)-2, 2);
	#print @temp;
    if (substr($_, length($_)-2, 2) eq ".p")
    {
		print "found .p file:\n";
		print $_;
		print "\n";
		#print "before open\n";
        open(IN, $_) or die "Can't open .p";
		#print "after open\n";
        open(OUT,">","temp.out") or die "Can't open temp.out"; 
		#print "after 2nd open\n";
    while (<IN>)
    {
	   #print "1\n";
	   #chomp($_);
	   ##################################################
	   #@lines = split("}");
	   #@size = length(@lines);
	   #print "size of array after split: ";
	   #print @size;
	   #print "\n";
	   #print "last array element is ";
	   #print @lines[@size-1];
	   #print "\n";
	   #if ((@size gt 1) && (@lines[@size-1] eq ";"))
	   #{
		#print "hit relevant line: ";
		#print $_;
		#print "\n";
	   #}
	   ##############################################
	   #@strim = chomp($_);
	   #print "next line after chomp:\n";
	   #print @strim;
	   #print "\n";
	   @strim = trim($_);
	   print "next line after trim:\n";
	   print @strim;
	   print "\n";
	   @temp = "};";
	   print "string for comparison:\n";
	   print @temp;
	   print "\n";
	   #TODO: for some reason, @comp is always 1!
	   @comp = @strim eq @temp;
	   print "comparison result:\n";
	   print @comp;
	   print "\n";
	   if (@comp)
	   {	
		print "inside if: string compare\n";
		@srtrim = rtrim($_);
		print "resulting string:\n";
		print substr(@srtrim, 0, length(@srtrim)-1_);
		print "\n";
		@res = substr(@srtrim, 0, length(@srtrim)-1_);
		print OUT @res;
	   }
	   else
	   {
	      print "else case \n";
		  print OUT $_; 
		  #print "after printing output\n";
	    }
	   #print <IN>;
       #@lines = split("}");
	   #@size = length(@s);
	   #print "size of the next line:\n";
	   #print @size;
	   #print "\n";
	   #@temp = substr(@s, @size-1, 2);
	   #print "comparing two last chars of the line:\n";
	   #print @temp;
	   #print "\n";
	   #print "2\n";
	   #TODO: condition below is wrong - we never go into "if"
	   #if ((@size gt 1) && (substr(<IN>, @size-2, 2) eq "};"))
       #{
		#print "3\n";
		#print "hit relevant line:";
		#print @s;
		#print "@lines[@size-1]:";
		#print @lines[@size-1];
		# remove trailing ; from @s and print
		#@size1 = length (@s);
		#@s = substr(@s, 0, @size1-1);
		
		#print "new string:";
		#print @s;
		
		#print OUT @s;
          
       #}
	   #else
	   #{
	      #print "else case \n";
		  #print OUT $_; 
		  #print "after printing output\n";
		 
	   #}
    }

    close IN;
    close OUT;
    system("move temp.out $_"); 
   }    

}

# Right trim function to remove trailing whitespace
sub rtrim($)
{
	my $string = shift;
	$string =~ s/\s+$//;
	return $string;
}
# Perl trim function to remove whitespace from the start and end of the string
sub trim($)
{
	my $string = shift;
	$string =~ s/^\s+//;
	$string =~ s/\s+$//;
	return $string;
}


find(\&wanted,$ARGV[0]); 