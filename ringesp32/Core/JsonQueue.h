#ifndef JSONQUEUE_H
#define JSONQUEUE_H

#include <ArduinoJson.h>

const int MAX = 5;

class JsonQueue {
  private:
    JsonDocument queue[MAX];
    int count = 0;

  public:
    void push(const JsonDocument& doc);
    JsonDocument pop();
    JsonDocument first();
    JsonDocument last();
    bool isEmpty() const;
    bool isFull() const;
    int size() const;

  JsonQueue() {
    count = 0;
    for (int i = 0; i < MAX; i++)
      queue[i] = "";
  }
};

#endif // JSONQUEUE_H
