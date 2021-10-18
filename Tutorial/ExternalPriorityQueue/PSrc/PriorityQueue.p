// Foreign type representing the Priority Queue.
// Note that you can use a P foreign type in a P program just like any other types in P.
type tPriorityQueue;

// Foreign Functions to perform operations on the Priority Queue

// Function to create an empty Priority Queue (default value of a foreign type is null)
fun CreatePriorityQueue(): tPriorityQueue;

// Function to add an element into the priority queue. Note that P does not support references and hence
// the function returns a priority queue after adding the element into the queue
fun AddElement(queue: tPriorityQueue, elem: any, priority: int): tPriorityQueue;

// Function to pick or choose a random element from the Priority Queue
fun ChooseElement(queue: tPriorityQueue): any;

// Function to get the element with highest priority in the queue and also the updated queue after
// removing this element from the queue.
fun RemoveElement(queue: tPriorityQueue): (element: any, queue: tPriorityQueue);

//Function to return the count of the number of elements in the Queue
fun CountElement(queue: tPriorityQueue): int;