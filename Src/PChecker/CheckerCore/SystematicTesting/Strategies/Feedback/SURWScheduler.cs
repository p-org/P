using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using PChecker.Generator.Object;
using PChecker.Random;
using PChecker.Runtime.StateMachines;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Strategies.Probabilistic;

namespace PChecker.SystematicTesting.Strategies.Feedback;

internal class SURWScheduler : IScheduler
{
    const ulong NoMachine = ulong.MaxValue;
    private readonly IRandomValueGenerator _randomValueGenerator;

    /// <summary>
    /// A mapping between machine ID and number of interesting events of that machine.
    /// </summary>
    private Dictionary<ulong, int> _executionLength;

    /// <summary>
    /// A mapping between machine ID and the number of interesting events left to execute on that machine.
    /// </summary>
    private Dictionary<ulong, int> _weights;

    /// <summary>
    /// A set of blocked machines.
    /// </summary>
    private HashSet<ulong> _blocked = new();

    /// <summary>
    /// The machine scheduled to execute next.
    /// </summary>
    private ulong _nextIntendedMachine = NoMachine;

    /// <summary>
    /// Machines that are created during the execution of the program.
    /// </summary>
    private HashSet<ulong> _createdMachines = new();

    /// <summary>
    /// A mapping between machine ID and the set of child machines that are created by that machine.
    /// </summary>
    private Dictionary<ulong, HashSet<ulong>> _childMachines;

    /// <summary>
    /// A set of machines from which events are uniformly sampled.
    /// </summary>
    private HashSet<ulong> _interestingMachines;

    // These fields are only used for the first trial to construct interesting operations map.

    private Dictionary<ulong, HashSet<ulong>> _interestingMachineMap = new();
    private Dictionary<ulong, int> _machineExecutionLengthCache = new();

    public SURWScheduler(Dictionary<ulong, int> executionLength, HashSet<ulong> interestingMachines,
        Dictionary<ulong, HashSet<ulong>> childMachines,
        IRandomValueGenerator random)
    {
        _executionLength = executionLength;
        _weights = _executionLength.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _randomValueGenerator = random;
        _interestingMachines = interestingMachines;
        _childMachines = childMachines;
    }

    private void CheckNewMachines(IEnumerable<AsyncOperation> ops)
    {
        foreach (var op in ops.Where(op => !_createdMachines.Contains(op.Id)))
        {
            _createdMachines.Add(op.Id);
            if (op is StateMachineOperation stateMachineOperation)
            {
                var parentMachine = stateMachineOperation.StateMachine.Creator;
                if (parentMachine == null) continue;
                _childMachines.TryAdd(parentMachine.Id.Value, new HashSet<ulong>());
                _childMachines[parentMachine.Id.Value].Add(stateMachineOperation.Id);
                var childWeight = _weights.GetValueOrDefault(op.Id, 0);
                var parentWeight = _weights.GetValueOrDefault(parentMachine.Id.Value, 0);
                if (_nextIntendedMachine == parentMachine.Id.Value)
                {
                    var randomWeight = _randomValueGenerator.Next(Math.Max(parentWeight, 1)) + 1;
                    if (childWeight > randomWeight)
                    {
                        _nextIntendedMachine = op.Id;
                    }
                }

                _weights[parentMachine.Id.Value] = Math.Max(parentWeight - childWeight, 0);
            }
        }
    }

    private void UpdateNextIntendedMachine(List<AsyncOperation> machines)
    {
        var totalWeight = machines.Sum(op => _weights.GetValueOrDefault(op.Id, 0));
        var selectedMachineWeight = _randomValueGenerator.Next(totalWeight) + 1;
        var currentWeight = 0;
        foreach (var op in machines)
        {
            var machineWeight = _weights.GetValueOrDefault(op.Id, 0);
            currentWeight += machineWeight;
            if (currentWeight >= selectedMachineWeight && (machineWeight != 0))
            {
                _nextIntendedMachine = op.Id;
                break;
            }
        }
    }

    private void ConstructInterestingOperation(AsyncOperation op)
    {
        if (op.Type == AsyncOperationType.Send)
        {
            _interestingMachineMap.TryAdd(op.MessageReceiver, new HashSet<ulong>());
            _interestingMachineMap[op.MessageReceiver].Add(op.Id);
        }
    }

    public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
    {
        next = null;
        var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
        if (enabledOperations.Count == 0)
        {
            return false;
        }

        CheckNewMachines(enabledOperations);
        var filteredMachines = enabledOperations.Where(op => !_blocked.Contains(op.Id)).ToList();
        if (filteredMachines.Count == 0)
        {
            _nextIntendedMachine = NoMachine;
            _blocked.Clear();
            return GetNextOperation(current, enabledOperations, out next);
        }

        var nextMachineIndex = _randomValueGenerator.Next(filteredMachines.Count);
        var nextMachine = filteredMachines[nextMachineIndex];

        if (nextMachine.Type == AsyncOperationType.Send && _interestingMachines.Contains(nextMachine.MessageReceiver))
        {
            if (_weights.GetValueOrDefault(nextMachine.Id, 0) == 0)
            {
                _weights[nextMachine.Id] = 1;
            }

            if (_nextIntendedMachine == NoMachine)
            {
                UpdateNextIntendedMachine(filteredMachines);
            }

            if (nextMachine.Id == _nextIntendedMachine)
            {
                _weights[nextMachine.Id] -= 1;
                _blocked.Clear();
                _nextIntendedMachine = NoMachine;
            }
            else
            {
                _blocked.Add(nextMachine.Id);
                return GetNextOperation(current, enabledOperations, out next);
            }

            if (_executionLength.Count == 0)
            {
                _machineExecutionLengthCache[nextMachine.Id] =
                    _machineExecutionLengthCache.GetValueOrDefault(nextMachine.Id, 0) + 1;
            }
        }

        // Unlike the original work, we need to construct the interesting machine map dynamically.
        if (_interestingMachines.Count == 0)
        {
            ConstructInterestingOperation(nextMachine);
        }

        next = nextMachine;
        return true;
    }

    private Dictionary<ulong, int> BuildMachineWeights()
    {
        var machineWeights = new Dictionary<ulong, int>();
        if (_machineExecutionLengthCache.Count != 0)
        {
            foreach (var machine in _createdMachines)
            {
                if (machineWeights.ContainsKey(machine)) continue;
                BuildMachineWeightsRecursive(machine, machineWeights);
            }
        }

        return machineWeights;
    }

    private int BuildMachineWeightsRecursive(ulong machine, Dictionary<ulong, int> machineWeights)
    {
        if (!machineWeights.ContainsKey(machine))
        {
            var weight = _machineExecutionLengthCache.GetValueOrDefault(machine, 0) +
                         _childMachines.GetValueOrDefault(machine, new HashSet<ulong>()).Sum(childMachine =>
                             BuildMachineWeightsRecursive(childMachine, machineWeights));
            machineWeights.Add(machine, weight);
        }

        return machineWeights[machine];
    }

    private HashSet<ulong> BuildInterestingMachines()
    {
        var keys = _interestingMachineMap.Where(machine => machine.Value.Count > 2).Select(machine => machine.Key)
            .ToList();
        var interestingMachines = keys.OrderBy(k => _randomValueGenerator.Next()).ToList();
        var count = Math.Max((int)(interestingMachines.Count * 0.1), 1);
        return interestingMachines.Take(count).ToHashSet();
    }

    public void Reset()
    {
        _executionLength.Clear();
        _interestingMachines.Clear();
    }

    public bool PrepareForNextIteration()
    {
        var newExecutionLength = _executionLength;
        if (newExecutionLength.Count == 0)
        {
            newExecutionLength = BuildMachineWeights();
        }

        var newInterestingMachines = _interestingMachines;
        if (newInterestingMachines.Count == 0)
        {
            newInterestingMachines = BuildInterestingMachines();
        }
        
        _executionLength = newExecutionLength;
        _interestingMachines = newInterestingMachines;
        
        _weights = _executionLength.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _blocked.Clear();
        _nextIntendedMachine = NoMachine;
        _createdMachines.Clear();
        _interestingMachineMap.Clear();
        _machineExecutionLengthCache.Clear();
        return true;
    }

    public IScheduler Mutate()
    {
        var newExecutionLength = _executionLength;
        if (newExecutionLength.Count == 0)
        {
            newExecutionLength = BuildMachineWeights();
        }

        var newInterestingMachines = _interestingMachines;
        if (newInterestingMachines.Count == 0)
        {
            newInterestingMachines = BuildInterestingMachines();
        }

        return new SURWScheduler(
            newExecutionLength,
            newInterestingMachines,
            _childMachines,
            ((ControlledRandom)_randomValueGenerator).Mutate()
        );
    }

    public IScheduler New()
    {
        var newExecutionLength = _executionLength;
        if (newExecutionLength.Count == 0)
        {
            newExecutionLength = BuildMachineWeights();
        }

        var newInterestingMachines = _interestingMachines;
        if (newInterestingMachines.Count == 0)
        {
            newInterestingMachines = BuildInterestingMachines();
        }
        
        return new SURWScheduler(
            newExecutionLength,
            newInterestingMachines,
            _childMachines,
            ((ControlledRandom)_randomValueGenerator).Mutate()
        );
    }
}