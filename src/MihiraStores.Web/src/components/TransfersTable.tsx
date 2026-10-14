import React, { useState } from 'react';
import type { StockTransfer, Branch } from '../types';
import { normalizeStatus } from '../types';
import {
  ArrowRight,
  Send,
  CheckCircle2,
  XCircle,
  Clock,
  Filter,
  PackageCheck,
  RefreshCw,
  Calendar,
} from 'lucide-react';

interface TransfersTableProps {
  transfers: StockTransfer[];
  branches: Branch[];
  isLoading: boolean;
  actionLoadingId: number | null;
  onDispatch: (id: number) => Promise<void>;
  onReceive: (id: number) => Promise<void>;
  onCancel: (id: number) => Promise<void>;
}

export const TransfersTable: React.FC<TransfersTableProps> = ({
  transfers,
  branches,
  isLoading,
  actionLoadingId,
  onDispatch,
  onReceive,
  onCancel,
}) => {
  const [selectedBranchFilter, setSelectedBranchFilter] = useState<number | 'all'>('all');
  const [selectedStatusFilter, setSelectedStatusFilter] = useState<string>('all');

  const filteredTransfers = transfers.filter((t) => {
    const status = normalizeStatus(t.status);
    if (selectedStatusFilter !== 'all' && status !== selectedStatusFilter) {
      return false;
    }
    if (selectedBranchFilter !== 'all') {
      return t.fromBranchId === selectedBranchFilter || t.toBranchId === selectedBranchFilter;
    }
    return true;
  });

  const getStatusBadge = (statusValue: StockTransfer['status']) => {
    const status = normalizeStatus(statusValue);
    switch (status) {
      case 'Requested':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200">
            <Clock className="w-3 h-3" />
            <span>Requested</span>
          </span>
        );
      case 'Dispatched':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-200">
            <Send className="w-3 h-3" />
            <span>Dispatched</span>
          </span>
        );
      case 'Received':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
            <CheckCircle2 className="w-3 h-3" />
            <span>Received</span>
          </span>
        );
      case 'Cancelled':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-slate-100 text-slate-600 border border-slate-300">
            <XCircle className="w-3 h-3" />
            <span>Cancelled</span>
          </span>
        );
    }
  };

  const formatDate = (isoString: string | null) => {
    if (!isoString) return null;
    try {
      const date = new Date(isoString);
      return date.toLocaleDateString(undefined, {
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return isoString;
    }
  };

  return (
    <div className="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
      {/* Table Header & Controls */}
      <div className="p-4 sm:p-5 border-b border-slate-200 bg-slate-50 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h2 className="text-lg font-bold text-slate-900 m-0">Stock Transfers</h2>
          <p className="text-xs text-slate-500 mt-1">
            Track, dispatch, receive, and manage all inventory movement requests.
          </p>
        </div>

        {/* Filters */}
        <div className="flex flex-wrap items-center gap-2">
          {/* Status Tabs */}
          <div className="inline-flex rounded-lg bg-slate-200/70 p-1 text-xs font-medium text-slate-600">
            {['all', 'Requested', 'Dispatched', 'Received', 'Cancelled'].map((st) => (
              <button
                key={st}
                type="button"
                onClick={() => setSelectedStatusFilter(st)}
                className={`px-3 py-1 rounded-md transition ${
                  selectedStatusFilter === st
                    ? 'bg-white text-slate-900 font-semibold shadow-2xs'
                    : 'hover:text-slate-900'
                }`}
              >
                {st === 'all' ? 'All Status' : st}
              </button>
            ))}
          </div>

          {/* Branch Filter */}
          <div className="flex items-center gap-1.5 bg-white border border-slate-300 rounded-lg px-2.5 py-1 shadow-2xs">
            <Filter className="w-3.5 h-3.5 text-slate-400" />
            <select
              value={selectedBranchFilter}
              onChange={(e) =>
                setSelectedBranchFilter(e.target.value === 'all' ? 'all' : Number(e.target.value))
              }
              className="text-xs font-medium text-slate-700 bg-transparent border-none focus:ring-0 cursor-pointer pr-4"
            >
              <option value="all">All Branches</option>
              {branches.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name} ({b.code})
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Table Body */}
      <div>
        {isLoading ? (
          <div className="py-16 text-center text-slate-500 flex flex-col items-center justify-center gap-2">
            <RefreshCw className="w-7 h-7 animate-spin text-indigo-600" />
            <span className="text-sm font-medium">Loading transfer records...</span>
          </div>
        ) : filteredTransfers.length === 0 ? (
          <div className="py-16 text-center text-slate-400 flex flex-col items-center justify-center gap-2">
            <PackageCheck className="w-10 h-10 text-slate-300" />
            <h3 className="text-sm font-bold text-slate-700 m-0">No Transfers Found</h3>
            <p className="text-xs text-slate-500 max-w-sm">
              {transfers.length === 0
                ? 'No stock transfers have been created yet. Use the transfer form above to create your first transfer.'
                : 'No stock transfers match your current filter selections.'}
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50/50 text-slate-500 uppercase tracking-wider font-semibold">
                  <th className="py-3 px-4">Transfer ID</th>
                  <th className="py-3 px-4">Route</th>
                  <th className="py-3 px-4">Items Transferred</th>
                  <th className="py-3 px-4">Timestamps</th>
                  <th className="py-3 px-4">Status</th>
                  <th className="py-3 px-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredTransfers.map((t) => {
                  const status = normalizeStatus(t.status);
                  const isOperating = actionLoadingId === t.id;
                  const isAnyActionRunning = actionLoadingId !== null;

                  return (
                    <tr key={t.id} className="hover:bg-slate-50/70 transition">
                      {/* ID */}
                      <td className="py-3.5 px-4 font-mono font-bold text-slate-900 whitespace-nowrap">
                        #{t.id}
                      </td>

                      {/* Route */}
                      <td className="py-3.5 px-4 whitespace-nowrap">
                        <div className="flex items-center gap-2 font-medium text-slate-800">
                          <span className="font-semibold text-slate-900">{t.fromBranchName}</span>
                          <ArrowRight className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                          <span className="font-semibold text-indigo-700">{t.toBranchName}</span>
                        </div>
                      </td>

                      {/* Items */}
                      <td className="py-3.5 px-4">
                        <div className="space-y-1">
                          {t.lines.map((l) => (
                            <div key={l.id} className="flex items-center gap-1.5 text-slate-700">
                              <span className="font-semibold text-slate-900">{l.quantity}x</span>
                              <span>{l.productName}</span>
                              <span className="text-[10px] font-mono text-slate-400">({l.productSku})</span>
                            </div>
                          ))}
                        </div>
                      </td>

                      {/* Timestamps */}
                      <td className="py-3.5 px-4 text-slate-500 whitespace-nowrap">
                        <div className="flex items-center gap-1 text-[11px]">
                          <Calendar className="w-3 h-3 text-slate-400" />
                          <span>Created: {formatDate(t.createdAtUtc)}</span>
                        </div>
                        {t.dispatchedAtUtc && (
                          <div className="text-[10px] text-blue-600 mt-0.5">
                            Dispatched: {formatDate(t.dispatchedAtUtc)}
                          </div>
                        )}
                        {t.receivedAtUtc && (
                          <div className="text-[10px] text-emerald-600 mt-0.5">
                            Received: {formatDate(t.receivedAtUtc)}
                          </div>
                        )}
                        {t.cancelledAtUtc && (
                          <div className="text-[10px] text-slate-500 mt-0.5">
                            Cancelled: {formatDate(t.cancelledAtUtc)}
                          </div>
                        )}
                      </td>

                      {/* Status Badge */}
                      <td className="py-3.5 px-4 whitespace-nowrap">
                        {getStatusBadge(t.status)}
                      </td>

                      {/* Actions */}
                      <td className="py-3.5 px-4 text-right whitespace-nowrap">
                        {status === 'Requested' && (
                          <div className="flex items-center justify-end gap-2">
                            <button
                              type="button"
                              onClick={() => onDispatch(t.id)}
                              disabled={isAnyActionRunning}
                              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 active:bg-blue-800 disabled:opacity-40 disabled:cursor-not-allowed shadow-2xs transition"
                              title="Validate stock and deduct inventory from source branch"
                            >
                              {isOperating ? (
                                <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                              ) : (
                                <Send className="w-3.5 h-3.5" />
                              )}
                              <span>Dispatch</span>
                            </button>

                            <button
                              type="button"
                              onClick={() => onCancel(t.id)}
                              disabled={isAnyActionRunning}
                              className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-semibold text-rose-700 bg-rose-50 border border-rose-200 hover:bg-rose-100 active:bg-rose-200 disabled:opacity-40 disabled:cursor-not-allowed transition"
                              title="Cancel transfer without stock deduction"
                            >
                              {isOperating ? (
                                <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                              ) : (
                                <XCircle className="w-3.5 h-3.5" />
                              )}
                              <span>Cancel</span>
                            </button>
                          </div>
                        )}

                        {status === 'Dispatched' && (
                          <button
                            type="button"
                            onClick={() => onReceive(t.id)}
                            disabled={isAnyActionRunning}
                            className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 disabled:opacity-40 disabled:cursor-not-allowed shadow-2xs transition"
                            title="Add stock to receiving branch and mark transfer Received"
                          >
                            {isOperating ? (
                              <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                            ) : (
                              <CheckCircle2 className="w-3.5 h-3.5" />
                            )}
                            <span>Receive</span>
                          </button>
                        )}

                        {status === 'Received' && (
                          <span className="inline-flex items-center gap-1 text-xs font-medium text-emerald-700 bg-emerald-50 px-2.5 py-1 rounded-md border border-emerald-100">
                            <CheckCircle2 className="w-3.5 h-3.5" />
                            <span>Completed</span>
                          </span>
                        )}

                        {status === 'Cancelled' && (
                          <span className="inline-flex items-center gap-1 text-xs font-medium text-slate-500 bg-slate-50 px-2.5 py-1 rounded-md border border-slate-200">
                            <XCircle className="w-3.5 h-3.5" />
                            <span>Voided</span>
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
