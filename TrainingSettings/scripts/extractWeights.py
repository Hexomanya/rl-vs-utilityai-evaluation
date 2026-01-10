import numpy as np
import sys

# Load the weight matrix
if len(sys.argv) > 1:
    weights_file = sys.argv[1]
else:
    weights_file = 'gemm_weights.npy'

print(f"Loading weights from: {weights_file}")
weights = np.load(weights_file)

print(f"Weight matrix shape: {weights.shape}")
print(f"Weight matrix dimensions: {weights.ndim}D")

# Handle different weight matrix orientations
# Common formats: (output_neurons, input_features) or (input_features, output_neurons)
if weights.ndim == 2:
    # ML-Agents ONNX typically uses (output_neurons, input_features)
    # For shape (256, 258): 256 output neurons, 258 input features
    print(f"\nInterpretation: {weights.shape[0]} output neurons, {weights.shape[1]} input features")
    print(f"Analyzing importance of {weights.shape[1]} observation inputs")
    
    # Each row is a neuron, each column is an input feature
    # We want to know which columns (inputs) have high weights
    analysis_weights = weights
    num_inputs = weights.shape[1]
    
    # Calculate importance metrics for each input
    mean_abs_weights = np.mean(np.abs(analysis_weights), axis=0)
    max_abs_weights = np.max(np.abs(analysis_weights), axis=0)
    std_weights = np.std(analysis_weights, axis=0)
    
elif weights.ndim == 1:
    # Bias vector or flattened weights
    num_inputs = weights.shape[0]
    mean_abs_weights = np.abs(weights)
    max_abs_weights = np.abs(weights)
    std_weights = np.zeros_like(weights)
else:
    print(f"Warning: Unexpected {weights.ndim}D weight tensor")
    # Flatten to 2D for analysis
    original_shape = weights.shape
    weights = weights.reshape(weights.shape[0], -1)
    num_inputs = weights.shape[1]
    mean_abs_weights = np.mean(np.abs(weights), axis=0)
    max_abs_weights = np.max(np.abs(weights), axis=0)
    std_weights = np.std(weights, axis=0)
    print(f"Reshaped from {original_shape} to {weights.shape} for analysis")

# Normalize importance scores (0-100 scale)
normalized_importance = 100 * mean_abs_weights / (np.max(mean_abs_weights) + 1e-10)

# Prepare results
results = []
results.append("=" * 70)
results.append(f"ML-Agents Weight Importance Analysis")
results.append(f"Weight file: {weights_file}")
results.append(f"Weight shape: {weights.shape}")
results.append("=" * 70)
results.append("")
results.append(f"{'Input':<8} {'Mean |W|':<12} {'Max |W|':<12} {'Std':<12} {'Norm%':<8} {'Status'}")
results.append("-" * 70)

threshold_mean = np.mean(mean_abs_weights)
threshold_low = threshold_mean * 0.1  # 10% of mean = likely ignored

for i in range(num_inputs):
    status = "IGNORED" if mean_abs_weights[i] < threshold_low else "USED"
    if mean_abs_weights[i] > threshold_mean * 2:
        status = "IMPORTANT"
    
    results.append(
        f"Input {i:<3} {mean_abs_weights[i]:<12.6f} {max_abs_weights[i]:<12.6f} "
        f"{std_weights[i]:<12.6f} {normalized_importance[i]:<8.2f} {status}"
    )

results.append("-" * 70)
results.append("")
results.append("Summary:")
results.append(f"  Total inputs: {num_inputs}")
results.append(f"  Mean importance: {np.mean(mean_abs_weights):.6f}")
results.append(f"  Threshold for 'ignored' (10% of mean): {threshold_low:.6f}")

ignored_inputs = np.where(mean_abs_weights < threshold_low)[0]
important_inputs = np.where(mean_abs_weights > threshold_mean * 2)[0]

results.append(f"  Potentially ignored inputs: {list(ignored_inputs)}")
results.append(f"  Important inputs: {list(important_inputs)}")
results.append("")
results.append("Ranking (most to least important):")
sorted_indices = np.argsort(mean_abs_weights)[::-1]
for rank, idx in enumerate(sorted_indices[:10], 1):  # Top 10
    results.append(f"  {rank}. Input {idx}: {mean_abs_weights[idx]:.6f} ({normalized_importance[idx]:.1f}%)")

# Print to console
for line in results:
    print(line)

# Export to file
output_file = weights_file.replace('.npy', '_analysis.txt')
with open(output_file, 'w') as f:
    f.write('\n'.join(results))

print(f"\n✓ Results exported to: {output_file}")

# Also export CSV for spreadsheet analysis
csv_file = weights_file.replace('.npy', '_analysis.csv')
with open(csv_file, 'w') as f:
    f.write("Input,Mean_Abs_Weight,Max_Abs_Weight,Std,Normalized_Importance,Status\n")
    for i in range(num_inputs):
        status = "IGNORED" if mean_abs_weights[i] < threshold_low else "USED"
        if mean_abs_weights[i] > threshold_mean * 2:
            status = "IMPORTANT"
        f.write(f"{i},{mean_abs_weights[i]:.6f},{max_abs_weights[i]:.6f},"
                f"{std_weights[i]:.6f},{normalized_importance[i]:.2f},{status}\n")

print(f"✓ CSV exported to: {csv_file}")