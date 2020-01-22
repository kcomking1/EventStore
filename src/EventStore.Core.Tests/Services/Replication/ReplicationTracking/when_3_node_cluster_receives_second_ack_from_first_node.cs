﻿using System;
using EventStore.Core.Messages;
using NUnit.Framework;

namespace EventStore.Core.Tests.Services.Replication.ReplicationTracking {
	[TestFixture]
	public class when_3_node_cluster_receives_second_ack_from_first_node :
		with_clustered_replication_tracking_service {
		private readonly long _firstLogPosition = 2000;
		private readonly long _secondLogPosition = 4000;
		private readonly Guid _slave1 = Guid.NewGuid();
		private readonly Guid _slave2 = Guid.NewGuid();

		public override void When() {
			BecomeMaster();
			// All of the nodes have acked the first write
			WriterCheckpoint.Write(_firstLogPosition);
			WriterCheckpoint.Flush();
			Service.Handle(new ReplicationTrackingMessage.WriterCheckpointFlushed());
			Service.Handle(new ReplicationTrackingMessage.ReplicaWriteAck(_slave1, _firstLogPosition));
			Service.Handle(new ReplicationTrackingMessage.ReplicaWriteAck(_slave2, _firstLogPosition));
			AssertEx.IsOrBecomesTrue(() => Service.IsIdle());

			ReplicatedTos.Clear();
			
			// Slave 2 has lost connection and does not ack the write
			WriterCheckpoint.Write(_secondLogPosition);
			WriterCheckpoint.Flush();
			Service.Handle(new ReplicationTrackingMessage.WriterCheckpointFlushed());
			Service.Handle(new ReplicationTrackingMessage.ReplicaWriteAck(_slave1, _secondLogPosition));
			AssertEx.IsOrBecomesTrue(() => Service.IsIdle());
		}

		[Test]
		public void replicated_to_should_be_sent_for_the_second_position() {
			Assert.True(ReplicatedTos.TryDequeue(out var msg));
			Assert.AreEqual(_secondLogPosition, msg.LogPosition);
		}

		[Test]
		public void replication_checkpoint_should_advance() {
			Assert.AreEqual(_secondLogPosition, ReplicationCheckpoint.Read());
			Assert.AreEqual(_secondLogPosition, ReplicationCheckpoint.ReadNonFlushed());
		}
	}
}