import React from 'react';
import { AlertCircle, CheckCircle2, X } from 'lucide-react';

interface BannerAlertProps {
  type: 'success' | 'error';
  message: string;
  onClose: () => void;
}

export const BannerAlert: React.FC<BannerAlertProps> = ({ type, message, onClose }) => {
  if (!message) return null;

  const isError = type === 'error';

  return (
    <div
      className={`rounded-xl border p-4 shadow-sm mb-6 flex items-start gap-3 transition-all ${
        isError
          ? 'bg-rose-50 border-rose-200 text-rose-900'
          : 'bg-emerald-50 border-emerald-200 text-emerald-900'
      }`}
      role="alert"
    >
      <div className="shrink-0 mt-0.5">
        {isError ? (
          <AlertCircle className="w-5 h-5 text-rose-600" />
        ) : (
          <CheckCircle2 className="w-5 h-5 text-emerald-600" />
        )}
      </div>

      <div className="flex-1 text-sm font-medium leading-relaxed">
        {message}
      </div>

      <button
        type="button"
        onClick={onClose}
        className={`p-1 rounded-md transition hover:bg-black/5 ${
          isError ? 'text-rose-600 hover:text-rose-800' : 'text-emerald-600 hover:text-emerald-800'
        }`}
        aria-label="Dismiss message"
      >
        <X className="w-4 h-4" />
      </button>
    </div>
  );
};
