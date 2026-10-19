# crossbeam-queue

[![NuGet](https://img.shields.io/nuget/v/crossbeam-queue.svg?style=flat-square)](https://www.nuget.org/packages/crossbeam-queue/)

Concurrent multi-producer, multi-consumer queues for C#.

## Choose a queue

| Type            | Storage                   | Behavior                                                                                                                             |
|-----------------|---------------------------|--------------------------------------------------------------------------------------------------------------------------------------|
| `ArrayQueue<T>` | Fixed-capacity array      | `TryEnqueue` returns `false` when full. `Enqueue` replaces the oldest element when full and reports the replaced value.              |
| `SegQueue<T>`   | Linked, fixed-size blocks | Grows as needed. Its constructor capacity limits how many unused blocks are retained for reuse, not how many elements can be queued. |

Both constructors require a positive capacity.

## `ArrayQueue<T>`

Use `ArrayQueue<T>` when the number of queued elements must stay within a fixed bound.
`TryEnqueue` leaves a full queue unchanged and returns `false`. `Enqueue` always adds the new
element; when full, it removes the oldest element and returns it in an `EnqueueResult`.

```csharp
using System;
using crossbeam;

var queue = new ArrayQueue<string>(capacity: 2);
queue.TryEnqueue("first");
queue.TryEnqueue("second");

ArrayQueue<string>.EnqueueResult result = queue.Enqueue("third");
if (result.HasOverwritten)
    Console.WriteLine($"Replaced: {result.Overwritten}");

while (queue.TryDequeue(out var item))
    Console.WriteLine(item);
```

Use `EnqueueResult.HasOverwritten` to determine whether an element was replaced. The replaced
value may itself be `null` or the default value of `T`.

## `SegQueue<T>`

Use `SegQueue<T>` when there is no fixed element capacity. It allocates additional blocks as
needed. Its constructor argument sets the maximum number of unused blocks kept for reuse; blocks
beyond that limit are not retained in the reuse pool.

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

Both queue types support concurrent producers and consumers. Both expose `Count` and `IsEmpty`;
`ArrayQueue<T>` also exposes `Capacity` and `IsFull`. These properties report the state observed
when read, and another operation may change the queue immediately afterward. Do not use them to
reserve capacity or coordinate queue operations; use the result of `TryEnqueue` or `TryDequeue`
instead.

The queues accept `null` values. For `TryDequeue`, use the returned `bool` to distinguish an empty
queue from a dequeued `null` value.

## Rust implementations

- [`ArrayQueue`](https://github.com/crossbeam-rs/crossbeam/blob/main/crossbeam-queue/src/array_queue.rs)
- [`SegQueue`](https://github.com/crossbeam-rs/crossbeam/blob/main/crossbeam-queue/src/seg_queue.rs)

## License

Distributed under the MIT License.
