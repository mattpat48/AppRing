#include "JsonQueue.h"

void JsonQueue::push(const JsonDocument doc) {
  if (count >= 1) {
    for (int i = count-1; i >= 0; i--) {
      if (i != 0) 
        queue[i] = queue[i-1];
      else {
        queue[i+1] = queue[i];
        queue[i] = doc;
      }
    }
  }
  else {
    queue[0] = doc;
  }
  if (count < MAX)
    count++;
}




JsonDocument JsonQueue::pop() {
  if (count <= 0) {
    static JsonDocument emptydoc;
    return emptydoc;
  }
  JsonDocument toReturn = first();
  for (int i = 0; i < count; i++) {
    if (i != (count - 1))
      queue[i] = queue[i+1];
    else
      queue[i] = "";
  }
  count--;
  return toReturn;
}




JsonDocument JsonQueue::first() {
  if (count <= 0) {
    static JsonDocument emptyDoc;
    return emptyDoc;
  }
  return queue[0];
}




JsonDocument JsonQueue::last() {
  if (count <= 0) {
    static JsonDocument emptyDoc;
    return emptyDoc;
  }
  return queue[count - 1];
}




bool JsonQueue::isEmpty() const {
  return count == 0;
}




bool JsonQueue::isFull() const {
  return count == MAX;
}




int JsonQueue::size() const {
  return count;
}
