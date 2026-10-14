import React from 'react';
import type { Branch, Stock } from '../types';
import { Boxes, Warehouse, Store, RefreshCw, AlertTriangle } from 'lucide-react';

interface BranchStockViewProps {
  branch: Branch | null;
  stocks: Stock[];
  isLoading: boolean;
  onRefreshStock: () => void;
}

export const BranchStockView: React.FC<BranchStockViewProps> = ({
  branch,
  stocks,
  isLoading,
  onRefreshStock,
}) => {
  if (!branch) {
    return (
      <div className="bg-white rounded-xl border border-slate-200 p-6 shadow-xs text-center">
        <Boxes className="w-10 h-10 text-slate-300 mx-auto mb-2" />
        <h3 className="text-sm font-semibold text-slate-700">No Branch Selected</h3>
        <p className="text-xs text-slate-500 mt-1">
          Select a sending branch to view its real-time available stock levels.
        </p>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
      <div className="p-4 sm:p-5 border-b border-slate-200 bg-slate-50 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className={`p-2 rounded-lg ${branch.isWarehouse ? 'bg-amber-100 text-amber-800' : 'bg-blue-100 text-blue-800'}`}>
            {branch.isWarehouse ? <Warehouse className="w-5 h-5" /> : <Store className="w-5 h-5" />}
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-base font-bold text-slate-900 m-0">{branch.name}</h3>
              <span className="text-xs font-mono font-semibold px-2 py-0.5 rounded bg-white border border-slate-300 text-slate-700">
                {branch.code}
              </span>
              {branch.isWarehouse && (
                <span className="text-xs font-semibold px-2 py-0.5 rounded bg-amber-50 text-amber-700 border border-amber-200">
                  Warehouse
                </span>
              )}
            </div>
            <p className="text-xs text-slate-500 mt-0.5">
              Available Inventory at Source Location
            </p>
          </div>
        </div>

        <button
          type="button"
          onClick={onRefreshStock}
          disabled={isLoading}
          className="p-1.5 text-slate-500 hover:text-slate-800 hover:bg-slate-200 rounded-lg transition disabled:opacity-50"
          title="Refresh stock for this branch"
        >
          <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin text-indigo-600' : ''}`} />
        </button>
      </div>

      <div className="p-4 sm:p-5">
        {isLoading ? (
          <div className="py-8 flex flex-col items-center justify-center gap-2 text-slate-500">
            <RefreshCw className="w-6 h-6 animate-spin text-indigo-600" />
            <span className="text-xs font-medium">Loading branch stock...</span>
          </div>
        ) : stocks.length === 0 ? (
          <div className="py-6 text-center text-slate-500">
            <AlertTriangle className="w-8 h-8 text-amber-500 mx-auto mb-1" />
            <p className="text-xs font-medium">No stock records found for this branch.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="border-b border-slate-200 text-slate-500 uppercase tracking-wider font-semibold">
                  <th className="py-2 px-3">Product Name</th>
                  <th className="py-2 px-3">SKU</th>
                  <th className="py-2 px-3 text-right">Available Stock</th>
                  <th className="py-2 px-3 text-center">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {stocks.map((item) => {
                  const isOutOfStock = item.quantity <= 0;
                  const isLowStock = item.quantity > 0 && item.quantity < 20;

                  return (
                    <tr key={item.id} className="hover:bg-slate-50 transition">
                      <td className="py-2.5 px-3 font-semibold text-slate-800">
                        {item.productName}
                      </td>
                      <td className="py-2.5 px-3 font-mono text-slate-500">
                        {item.productSku}
                      </td>
                      <td className="py-2.5 px-3 text-right font-mono font-bold text-slate-900 text-sm">
                        {item.quantity.toLocaleString()}
                      </td>
                      <td className="py-2.5 px-3 text-center">
                        {isOutOfStock ? (
                          <span className="inline-block px-2 py-0.5 rounded text-[11px] font-semibold bg-rose-50 text-rose-700 border border-rose-200">
                            Out of Stock
                          </span>
                        ) : isLowStock ? (
                          <span className="inline-block px-2 py-0.5 rounded text-[11px] font-semibold bg-amber-50 text-amber-700 border border-amber-200">
                            Low Stock
                          </span>
                        ) : (
                          <span className="inline-block px-2 py-0.5 rounded text-[11px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
                            Available
                          </span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
