import React, { useState } from 'react';
import type { Branch, Product, Stock, CreateStockTransferRequest } from '../types';
import { Plus, Trash2, ArrowRight, AlertCircle } from 'lucide-react';

interface CreateTransferFormProps {
  branches: Branch[];
  products: Product[];
  fromBranchId: number | null;
  toBranchId: number | null;
  onFromBranchChange: (id: number) => void;
  onToBranchChange: (id: number) => void;
  availableStocks: Stock[];
  isSubmitting: boolean;
  onSubmitTransfer: (request: CreateStockTransferRequest) => Promise<void>;
}

interface FormLine {
  id: string; // unique key for react rendering
  productId: number;
  quantity: number;
}

export const CreateTransferForm: React.FC<CreateTransferFormProps> = ({
  branches,
  products,
  fromBranchId,
  toBranchId,
  onFromBranchChange,
  onToBranchChange,
  availableStocks,
  isSubmitting,
  onSubmitTransfer,
}) => {
  const [lines, setLines] = useState<FormLine[]>([
    { id: 'initial-line', productId: products[0]?.id || 1, quantity: 1 },
  ]);

  const getAvailableStockForProduct = (productId: number): number => {
    const stockItem = availableStocks.find((s) => s.productId === productId);
    return stockItem ? stockItem.quantity : 0;
  };

  const handleAddLine = () => {
    // Pick first product not already in lines, or fallback to first product
    const existingProductIds = new Set(lines.map((l) => l.productId));
    const nextProduct = products.find((p) => !existingProductIds.has(p.id)) || products[0];

    setLines((prev) => [
      ...prev,
      {
        id: `line-${Date.now()}-${Math.random()}`,
        productId: nextProduct ? nextProduct.id : 1,
        quantity: 1,
      },
    ]);
  };

  const handleRemoveLine = (id: string) => {
    setLines((prev) => prev.filter((l) => l.id !== id));
  };

  const handleProductChange = (id: string, productId: number) => {
    setLines((prev) =>
      prev.map((l) => (l.id === id ? { ...l, productId } : l))
    );
  };

  const handleQuantityChange = (id: string, quantity: number) => {
    setLines((prev) =>
      prev.map((l) => (l.id === id ? { ...l, quantity: Math.max(1, quantity) } : l))
    );
  };

  // Validations
  const isBranchSelectionValid =
    fromBranchId !== null &&
    toBranchId !== null &&
    fromBranchId !== toBranchId;

  const hasLines = lines.length > 0;
  const allQuantitiesPositive = lines.every((l) => l.quantity >= 1);
  const isValid = isBranchSelectionValid && hasLines && allQuantitiesPositive;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isValid || !fromBranchId || !toBranchId || isSubmitting) return;

    // Consolidate lines by product ID
    const consolidatedMap = new Map<number, number>();
    for (const line of lines) {
      consolidatedMap.set(
        line.productId,
        (consolidatedMap.get(line.productId) || 0) + line.quantity
      );
    }

    const consolidatedLines = Array.from(consolidatedMap.entries()).map(
      ([productId, quantity]) => ({ productId, quantity })
    );

    const payload: CreateStockTransferRequest = {
      fromBranchId,
      toBranchId,
      lines: consolidatedLines,
    };

    await onSubmitTransfer(payload);

    // Reset lines to default
    setLines([{ id: `line-${Date.now()}`, productId: products[0]?.id || 1, quantity: 1 }]);
  };

  return (
    <div className="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
      <div className="p-4 sm:p-5 border-b border-slate-200 bg-slate-50">
        <h2 className="text-lg font-bold text-slate-900 m-0">Create Stock Transfer</h2>
        <p className="text-xs text-slate-500 mt-1">
          Initiate an inter-branch inventory transfer in <span className="font-semibold text-amber-700">Requested</span> status.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="p-4 sm:p-6 space-y-6">
        {/* Branch Selection */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 items-center">
          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
              Source (Sending) Branch <span className="text-rose-500">*</span>
            </label>
            <select
              value={fromBranchId ?? ''}
              onChange={(e) => onFromBranchChange(Number(e.target.value))}
              disabled={isSubmitting}
              className="w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg shadow-2xs focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 disabled:bg-slate-100 disabled:cursor-not-allowed"
            >
              <option value="" disabled>Select sending branch...</option>
              {branches.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name} ({b.code}){b.isWarehouse ? ' - Warehouse' : ''}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
              Destination (Receiving) Branch <span className="text-rose-500">*</span>
            </label>
            <select
              value={toBranchId ?? ''}
              onChange={(e) => onToBranchChange(Number(e.target.value))}
              disabled={isSubmitting}
              className="w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg shadow-2xs focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 disabled:bg-slate-100 disabled:cursor-not-allowed"
            >
              <option value="" disabled>Select receiving branch...</option>
              {branches.map((b) => (
                <option key={b.id} value={b.id} disabled={b.id === fromBranchId}>
                  {b.name} ({b.code}){b.id === fromBranchId ? ' (Source Branch)' : ''}
                </option>
              ))}
            </select>
          </div>
        </div>

        {fromBranchId && toBranchId && fromBranchId === toBranchId && (
          <div className="p-3 bg-rose-50 border border-rose-200 rounded-lg flex items-center gap-2 text-xs font-medium text-rose-700">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>Sending and receiving branches cannot be the same. Please select a different destination.</span>
          </div>
        )}

        {/* Transfer Line Items */}
        <div>
          <div className="flex items-center justify-between mb-2">
            <div>
              <h3 className="text-xs font-bold text-slate-700 uppercase tracking-wider m-0">
                Transfer Items <span className="text-rose-500">*</span>
              </h3>
              <p className="text-[11px] text-slate-500">
                Add products and quantities to transfer. Available stock at the sending branch is shown.
              </p>
            </div>

            <button
              type="button"
              onClick={handleAddLine}
              disabled={isSubmitting}
              className="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-semibold text-indigo-700 bg-indigo-50 border border-indigo-200 rounded-md hover:bg-indigo-100 active:bg-indigo-200 disabled:opacity-50 transition"
            >
              <Plus className="w-3.5 h-3.5" />
              <span>Add Item</span>
            </button>
          </div>

          <div className="space-y-3">
            {lines.length === 0 ? (
              <div className="p-4 border-2 border-dashed border-slate-200 rounded-lg text-center text-xs text-slate-500">
                No items added yet. Click &quot;Add Item&quot; to include products in this transfer.
              </div>
            ) : (
              lines.map((line, idx) => {
                const available = getAvailableStockForProduct(line.productId);
                const exceedsStock = fromBranchId !== null && line.quantity > available;

                return (
                  <div
                    key={line.id}
                    className="p-3 bg-slate-50 border border-slate-200 rounded-lg flex flex-col sm:flex-row items-stretch sm:items-center gap-3 transition"
                  >
                    <div className="flex-1">
                      <label className="block text-[11px] font-medium text-slate-500 mb-1">
                        Item #{idx + 1} - Product
                      </label>
                      <select
                        value={line.productId}
                        onChange={(e) => handleProductChange(line.id, Number(e.target.value))}
                        disabled={isSubmitting}
                        className="w-full px-3 py-1.5 text-sm bg-white border border-slate-300 rounded-md shadow-2xs focus:ring-1 focus:ring-indigo-500 disabled:bg-slate-100"
                      >
                        {products.map((p) => (
                          <option key={p.id} value={p.id}>
                            {p.name} ({p.sku})
                          </option>
                        ))}
                      </select>
                    </div>

                    <div className="w-full sm:w-44">
                      <div className="flex items-center justify-between mb-1">
                        <label className="text-[11px] font-medium text-slate-500">
                          Quantity
                        </label>
                        {fromBranchId && (
                          <span
                            className={`text-[11px] font-semibold ${
                              exceedsStock ? 'text-rose-600' : 'text-slate-500'
                            }`}
                          >
                            Avail: {available}
                          </span>
                        )}
                      </div>
                      <input
                        type="number"
                        min="1"
                        value={line.quantity}
                        onChange={(e) => handleQuantityChange(line.id, parseInt(e.target.value) || 1)}
                        disabled={isSubmitting}
                        className={`w-full px-3 py-1.5 text-sm bg-white border rounded-md shadow-2xs focus:ring-1 disabled:bg-slate-100 font-mono ${
                          exceedsStock
                            ? 'border-rose-400 focus:ring-rose-500 text-rose-900'
                            : 'border-slate-300 focus:ring-indigo-500'
                        }`}
                      />
                    </div>

                    <div className="sm:self-end pt-1">
                      <button
                        type="button"
                        onClick={() => handleRemoveLine(line.id)}
                        disabled={isSubmitting || lines.length <= 1}
                        className="w-full sm:w-auto p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-md transition disabled:opacity-30 disabled:hover:bg-transparent disabled:hover:text-slate-400"
                        title={lines.length <= 1 ? 'Transfer must contain at least one item' : 'Remove this line'}
                      >
                        <Trash2 className="w-4 h-4 mx-auto" />
                      </button>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>

        {/* Action Button */}
        <div className="pt-2 flex flex-col sm:flex-row items-center justify-between gap-3 border-t border-slate-100">
          <div className="text-xs text-slate-500">
            {lines.length > 0 && (
              <span>
                Total Lines: <strong className="text-slate-800">{lines.length}</strong> | Total Units:{' '}
                <strong className="text-slate-800">
                  {lines.reduce((acc, l) => acc + (l.quantity || 0), 0)}
                </strong>
              </span>
            )}
          </div>

          <button
            type="submit"
            disabled={!isValid || isSubmitting}
            className="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-5 py-2.5 rounded-lg text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 shadow-sm disabled:opacity-50 disabled:cursor-not-allowed transition"
          >
            {isSubmitting ? (
              <>
                <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                <span>Creating Transfer...</span>
              </>
            ) : (
              <>
                <span>Submit Transfer Request</span>
                <ArrowRight className="w-4 h-4" />
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  );
};
