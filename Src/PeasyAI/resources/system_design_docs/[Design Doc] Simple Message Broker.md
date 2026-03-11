# Simple Message Broker

## Introduction

This document describes the design of a simplified distributed message broker inspired by Apache Kafka. The system models the core mechanics of a publish-subscribe log: producers write keyed messages into an append-only topic log managed by a central broker, and consumers independently poll that log, each tracking their own read position. The broker is the single source of truth for the message log and for each consumer's committed offset.

The design intentionally constrains itself to a single topic with a single partition. This keeps the state space tractable for model checking while still exercising the interesting protocol interactions — in particular, the interplay between concurrent producers racing to append, multiple consumers progressing at different speeds, and the offset-commit protocol that ensures consumers can resume from a consistent position after a restart.

**Assumptions:**
1. There is exactly one topic backed by a single ordered partition; multi-partition fan-out is out of scope.
2. The network is reliable: every message sent between components is eventually delivered, and there are no duplicates or reorderings at the transport level.
3. Consumers read the log sequentially. Each consumer maintains a local cursor that advances through the log; random seeks are not supported.
4. The broker acknowledges every publish synchronously before the producer moves on, giving us a clear linearization point for each write.
5. Message delivery follows a pull model: consumers poll the broker at their own pace rather than having messages pushed to them, mirroring Kafka's real consumer protocol.

## Components

### Source Components

#### 1. Broker
- **Role:** The heart of the system — owns an append-only log and manages consumer offsets.
- **States:** Init, Ready
- **Local state:**
    - `log`: append-only log of (key, value) records stamped with monotonically increasing offsets
    - `consumerOffsets`: last committed offset per registered consumer
    - `registeredConsumers`: set of consumers that have registered
    - `batchSize`: maximum number of records to return per poll
- **Initialization:** Created with the maximum batch size for poll responses.
- **Behavior:**
    - When a producer publishes a message, appends it to the tail of the log and replies with the assigned offset.
    - Maintains a registry of known consumers. A consumer must register before it can issue its first poll.
    - Tracks each consumer's last committed offset so that, if the consumer restarts, it can resume from where it left off.
    - When a consumer polls, slices the log from the requested offset forward (up to batchSize) and returns the batch.
    - On offset-commit, persists the new offset and acknowledges.

#### 2. Producer
- **Role:** Publishes a fixed number of key-value messages to the broker, one at a time.
- **States:** Init, Publishing, Done
- **Local state:**
    - `broker`: reference to the broker
    - `numMessages`: number of messages to publish
    - `messagesSent`: count of messages sent so far
- **Initialization:** Created with a reference to the broker and the number of messages to publish.
- **Behavior:**
    - After sending each publish request, blocks until it receives the broker's acknowledgment, ensuring messages from a single producer are appended in order.

### Test Components

#### 3. Consumer
- **Role:** Registers with the broker and polls for messages in a loop.
- **States:** Init, Registering, Polling, Processing, Done
- **Local state:**
    - `broker`: reference to the broker
    - `currentOffset`: current read position in the log
    - `targetCount`: number of messages to consume before stopping
    - `consumed`: count of messages consumed so far
- **Initialization:** Created with a reference to the broker and the target number of messages to consume.
- **Behavior:**
    - Begins by registering with the broker, then enters a poll loop.
    - On each iteration, asks the broker for the next batch of messages starting from its current offset.
    - Processes messages in order and commits the new offset back to the broker before polling again.
    - Validates that the offsets received are strictly increasing.
    - The loop terminates once the consumer has consumed the target number of messages.

## Interactions

1. **ePublish**
    - **Source:** Producer
    - **Target:** Broker
    - **Payload:** the producer's reference, the message key, and the message value
    - **Description:** A producer sends ePublish to write a single keyed message into the topic. The broker appends the record to the tail of its log, assigns the next sequential offset, and replies with ePublishAck.
    - **Effects:**
        - Broker appends the record and assigns a monotonically increasing offset.
        - Broker replies with ePublishAck containing the assigned offset.

2. **ePublishAck**
    - **Source:** Broker
    - **Target:** Producer
    - **Payload:** the assigned offset in the log
    - **Description:** The broker's confirmation that the message has been durably appended. The offset tells the producer exactly where in the log its message landed.
    - **Effects:**
        - Producer is free to send its next message.

3. **ePoll**
    - **Source:** Consumer
    - **Target:** Broker
    - **Payload:** the consumer's reference and the starting offset to read from
    - **Description:** A consumer issues a poll to request the next slice of the log starting at fromOffset.
    - **Effects:**
        - Broker reads from that position up to its internal batch-size limit and responds with ePollResp.
        - If fromOffset is already at or past the end of the log, the broker returns an empty batch.

4. **ePollResp**
    - **Source:** Broker
    - **Target:** Consumer
    - **Payload:** a batch of log records (each with offset, key, and value) and the end offset just past the last message
    - **Description:** The broker's response carrying a batch of log records. Each record includes its offset so the consumer can verify ordering. The endOffset indicates the offset just past the last message in the batch.
    - **Effects:**
        - Consumer processes messages and then issues an eCommitOffset.

5. **eCommitOffset**
    - **Source:** Consumer
    - **Target:** Broker
    - **Payload:** the consumer's reference and the offset to commit
    - **Description:** After processing a batch, the consumer commits its new read position. The broker persists the offset in its consumer registry.
    - **Effects:**
        - Broker records the committed offset and acknowledges with eCommitOffsetAck.

6. **eCommitOffsetAck**
    - **Source:** Broker
    - **Target:** Consumer
    - **Payload:** the committed offset
    - **Description:** Confirmation that the broker has recorded the consumer's committed offset.
    - **Effects:**
        - Consumer proceeds to its next poll iteration.

7. **eRegisterConsumer**
    - **Source:** Consumer
    - **Target:** Broker
    - **Payload:** the consumer's reference
    - **Description:** A one-time handshake that a consumer must complete before it can poll. The broker adds the consumer to its registry with an initial committed offset of zero.
    - **Effects:**
        - Broker registers the consumer and responds with eRegisterConsumerAck.

8. **eRegisterConsumerAck**
    - **Source:** Broker
    - **Target:** Consumer
    - **Payload:** none
    - **Description:** Acknowledgment that the consumer has been registered.
    - **Effects:**
        - Consumer transitions into its polling loop.

## Specifications

1. **OrderedDelivery** (safety property):
   Every ePollResp delivered to a consumer must carry offsets that are strictly greater than all offsets in any previously received ePollResp for that consumer. A violation would mean the broker is serving stale or out-of-order data.

2. **NoMessageLoss** (safety property):
   Every ePublish that the broker acknowledges with an ePublishAck must eventually appear in some ePollResp. The log must grow monotonically — an acknowledged offset must never disappear, and no gaps may appear in the offset sequence.

3. **ConsistentOffsets** (safety property):
   The offset in any eCommitOffset from a consumer must not exceed the highest offset assigned by any ePublishAck. Otherwise the consumer would believe it had read messages that do not yet exist.

## Test Scenarios

1. 1 producer, 1 consumer, 5 messages — the happy-path baseline that exercises the full publish, poll, commit cycle end to end.
2. 2 producers, 1 consumer, 3 messages each — tests that concurrent appends from different producers are correctly serialized in the log and that the consumer sees a consistent total order.
3. 1 producer, 2 consumers, 5 messages — verifies that independent consumers can progress through the same log at different rates without interfering with each other's offsets.
