// Foreign type representing the Priority Queue.
type tPriorityQueue;

/* Foreign functions to perform operations on the Priority Queue */

// Function to create an empty Priority Queue (default value of any foreign type is null)
fun CreatePriorityQueue(): tPriorityQueue;

// Function to add an element into the priority queue.
fun AddElement(queue: tPriorityQueue, elem: any, priority: int): tPriorityQueue;

// Function to pick or choose a random element from the Priority Queue
fun ChooseElement(queue: tPriorityQueue): any;

// Function to remove the element with highest priority in the queue and return the updated queue after
// removing this element from the queue.
fun RemoveElement(queue: tPriorityQueue): (element: any, queue: tPriorityQueue);

// Function to return the count of elements in the Queue
fun CountElement(queue: tPriorityQueue): int;