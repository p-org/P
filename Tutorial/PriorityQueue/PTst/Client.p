/*
Client using the Priority Queue
*/

machine Client
{
  var queue : tPriorityQueue;

  start state Init {
    entry {
      var element: string;

      queue = CreatePriorityQueue();

      // lets add some random elements in to the queue.
      queue = AddElement(queue, "Hello", 1);
      queue = AddElement(queue, "World", 2);
      queue = AddElement(queue, "!!", 3);

      // lets choose an element from the queue
      element = ChooseElement(queue) as string;

      // check if choose element is implemented correctly!
      assert element == "Hello" || element == "World" || element == "!!";

      // local foreign function that adds an int value to the queue with priority.
      queue = AddIntToQueue(queue, 123, 4);

      // print elements in priority order
      RemoveElementsInPriorityOrder();
    }
  }

  fun RemoveElementsInPriorityOrder() {
    var i: int;
    var retVal : (element : any, queue: tPriorityQueue);
    print "--------------";
    while(CountElement(queue) > 0) {
      retVal = RemoveElement(queue);
      queue = retVal.queue;
      print format ("{0}", retVal.element);
    }
    print "--------------";
  }

  // local foreign function that adds elements into the Queue;
  fun AddIntToQueue(queue: tPriorityQueue, elem: int, p: int): tPriorityQueue;
}