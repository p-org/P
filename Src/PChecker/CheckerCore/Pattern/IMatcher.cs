using System.Collections.Generic;
using PChecker.Matcher;

namespace PChecker.Feedback;

public interface IMatcher
{

    public int IsMatched(List<EventObj> events);
}