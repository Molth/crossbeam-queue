# crossbeam-queue

[![NuGet](https://img.shields.io/nuget/v/crossbeam-queue.svg?style=flat-square)](https://www.nuget.org/packages/crossbeam-queue/)

Concurrent multi-producer, multi-consumer queues for C#.

## `ArrayQueue<T>`

Use `ArrayQueue<T>` when the number of queued elements must stay within a fixed bound. Its
constructor requires a positive `capacity`, which sets the maximum number of queued elements.

- `TryEnqueue` leaves a full queue unchanged and returns `false`.
- `Enqueue` always adds the new element. If the queue is full, it removes the oldest element and
  reports it in an `EnqueueResult`.

```csharp
using System;
using crossbeam;

var queue = new ArrayQueue<string>(capacity: 2);
queue.TryEnqueue("first");
queue.TryEnqueue("second");

ArrayQueue<string>.EnqueueResult result = queue.Enqueue("third");
if (result.TryGetOverwritten(out var overwritten))
    Console.WriteLine($"Replaced: {overwritten}");

while (queue.TryDequeue(out var item))
    Console.WriteLine(item);
```

`EnqueueResult.TryGetOverwritten` returns `true` if an element was replaced. Otherwise, it returns
`false` and sets the output to `default`. Check the Boolean result: the replaced element itself may
be `null` or the default value of `T`.

## `SegQueue<T>`

Use `SegQueue<T>` when you need a queue without a fixed element-count limit.

- The queue allocates blocks as needed.
- Its constructor requires a positive `capacity`. This sets the maximum number of unused blocks
  retained for reuse; it does not limit queued elements. Blocks beyond that limit are not retained
  in the reuse pool.

```csharp
using System;
using crossbeam;

var queue = new SegQueue<string>(capacity: 4);
queue.Enqueue("first");
queue.Enqueue("second");

while (queue.TryDequeue(out var item))
    Console.WriteLine(item);
```

## Concurrent use

Both queue types support concurrent producers and consumers.

- `Count` and `IsEmpty` are snapshots and may change immediately. `ArrayQueue<T>` also exposes a
  fixed `Capacity` and snapshot `IsFull`. Use the result of `TryEnqueue` or `TryDequeue` to know
  whether that operation succeeded.
- `null` is a valid element. `TryDequeue` returns `true` and the removed value when successful; if
  the queue is empty, it returns `false` and sets the output to `default`. Use the Boolean result to
  distinguish an empty queue from one that returned `null`.

## Api scope

These queues are not drop-in replacements for `ConcurrentQueue<T>`. They focus on enqueue and
dequeue operations and do not provide:

- `TryPeek`
- `GetEnumerator`
- `Clear`

## Rust implementations

- [`ArrayQueue`](https://github.com/crossbeam-rs/crossbeam/blob/main/crossbeam-queue/src/array_queue.rs)
- [`SegQueue`](https://github.com/crossbeam-rs/crossbeam/blob/main/crossbeam-queue/src/seg_queue.rs)

## License

Distributed under the MIT License.
