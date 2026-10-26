import React, { useState, useEffect } from 'react';
import { ShieldAlert, X, RefreshCw, AlertTriangle, CheckCircle, Code, ChevronRight } from 'lucide-react';
import api from '../api';

export default function PlagiarismModal({ isOpen, onClose, examClass }) {
  const [threshold, setThreshold] = useState(70);
  const [loading, setLoading] = useState(false);
  const [report, setReport] = useState(null);
  const [error, setError] = useState('');
  const [selectedPair, setSelectedPair] = useState(null);

  useEffect(() => {
    if (isOpen && examClass) {
      setSelectedPair(null);
      fetchReport();
    }
  }, [isOpen, examClass]);

  const fetchReport = async () => {
    if (!examClass) return;
    setLoading(true);
    setError('');
    try {
      const res = await api.get(`/exam-classes/${examClass.id}/plagiarism-report`);
      setReport(res.data);
    } catch (err) {
      console.error(err);
      // If 404 or not found, report will remain null
      setReport(null);
    } finally {
      setLoading(false);
    }
  };

  const handleRunScan = async () => {
    if (!examClass) return;
    setLoading(true);
    setError('');
    try {
      const thresholdRatio = Number(threshold) / 100;
      const res = await api.post(`/exam-classes/${examClass.id}/check-plagiarism?threshold=${thresholdRatio}`);
      setReport(res.data);
      setSelectedPair(null);
    } catch (err) {
      console.error(err);
      setError(err.response?.data?.message || 'Có lỗi xảy ra khi quét trùng lặp mã nguồn.');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen || !examClass) return null;

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        background: 'rgba(0, 0, 0, 0.7)',
        backdropFilter: 'blur(6px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1200,
        padding: '1rem'
      }}
    >
      <div
        className="glass-panel fade-in"
        style={{
          width: '100%',
          maxWidth: '920px',
          maxHeight: '90vh',
          display: 'flex',
          flexDirection: 'column',
          border: '1px solid var(--panel-border)',
          overflow: 'hidden'
        }}
      >
        {/* Modal Header */}
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            padding: '1.2rem 1.5rem',
            borderBottom: '1px solid var(--panel-border)',
            background: 'rgba(255, 255, 255, 0.02)'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
            <div
              style={{
                width: '36px',
                height: '36px',
                borderRadius: '8px',
                background: 'rgba(239, 68, 68, 0.15)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#ef4444'
              }}
            >
              <ShieldAlert size={20} />
            </div>
            <div>
              <h3 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>
                Quét Đạo Code & Trùng Lặp (Winnowing Algorithm)
              </h3>
              <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                Lớp thi: <b style={{ color: 'var(--color-primary)' }}>{examClass.classCode}</b> - {examClass.subjectCode}
              </span>
            </div>
          </div>

          <button
            onClick={onClose}
            style={{
              background: 'none',
              border: 'none',
              color: 'var(--text-muted)',
              cursor: 'pointer',
              padding: '0.4rem',
              borderRadius: '6px'
            }}
          >
            <X size={20} />
          </button>
        </div>

        {/* Control Bar: Threshold & Trigger */}
        <div
          style={{
            padding: '1rem 1.5rem',
            background: 'rgba(255, 255, 255, 0.01)',
            borderBottom: '1px solid var(--panel-border)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            flexWrap: 'wrap',
            gap: '1rem'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <label style={{ fontSize: '0.85rem', color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <span>Ngưỡng tương đồng tối thiểu:</span>
              <input
                type="number"
                min="30"
                max="99"
                value={threshold}
                onChange={(e) => setThreshold(e.target.value)}
                style={{
                  width: '65px',
                  padding: '0.3rem 0.5rem',
                  borderRadius: '6px',
                  border: '1px solid var(--panel-border)',
                  background: 'var(--panel-bg)',
                  color: 'var(--text-main)',
                  fontWeight: 700,
                  textAlign: 'center'
                }}
              />
              <span style={{ fontWeight: 600 }}>%</span>
            </label>
            <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
              (Khuyên dùng: 70% - phát hiện chia sẻ bài / đổi tên biến)
            </span>
          </div>

          <button
            type="button"
            onClick={handleRunScan}
            disabled={loading}
            className="btn btn-primary btn-sm"
            style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}
          >
            <RefreshCw size={14} className={loading ? 'spin' : ''} />
            {loading ? 'Đang so khớp Winnowing...' : 'Bắt đầu quét trùng lặp'}
          </button>
        </div>

        {error && (
          <div style={{ margin: '1rem 1.5rem 0', padding: '0.75rem 1rem', background: 'rgba(239, 68, 68, 0.1)', border: '1px solid rgba(239, 68, 68, 0.3)', borderRadius: '8px', color: '#ef4444', fontSize: '0.85rem' }}>
            {error}
          </div>
        )}

        {/* Modal Body */}
        <div style={{ padding: '1.25rem 1.5rem', overflowY: 'auto', flex: 1 }}>
          {loading && !report ? (
            <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
              <RefreshCw size={36} className="spin" style={{ margin: '0 auto 1rem', display: 'block', color: 'var(--color-primary)' }} />
              <p>Đang phân tích token và tạo fingerprint theo thuật toán Winnowing MOSS...</p>
            </div>
          ) : !report ? (
            <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
              <ShieldAlert size={48} style={{ strokeWidth: 1.2, margin: '0 auto 1rem', display: 'block', color: 'var(--text-muted)' }} />
              <p style={{ fontSize: '1rem', fontWeight: 600, color: 'var(--text-main)' }}>Chưa có báo cáo đạo code cho lớp thi này</p>
              <p style={{ fontSize: '0.85rem' }}>Nhấn nút <b>"Bắt đầu quét trùng lặp"</b> ở trên để hệ thống kiểm tra chéo toàn bộ bài nộp.</p>
            </div>
          ) : (
            <>
              {/* Summary Metrics */}
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
                  gap: '0.75rem',
                  marginBottom: '1.25rem'
                }}
              >
                <div style={{ background: 'rgba(255, 255, 255, 0.03)', border: '1px solid var(--panel-border)', borderRadius: '8px', padding: '0.85rem' }}>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.2rem' }}>Tổng bài nộp</div>
                  <div style={{ fontSize: '1.25rem', fontWeight: 700 }}>{report.totalSubmissions}</div>
                </div>
                <div style={{ background: 'rgba(255, 255, 255, 0.03)', border: '1px solid var(--panel-border)', borderRadius: '8px', padding: '0.85rem' }}>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.2rem' }}>Cặp bài đã đối chiếu</div>
                  <div style={{ fontSize: '1.25rem', fontWeight: 700 }}>{report.totalPairsChecked}</div>
                </div>
                <div
                  style={{
                    background: report.suspectedPairsCount > 0 ? 'rgba(239, 68, 68, 0.08)' : 'rgba(34, 197, 94, 0.08)',
                    border: `1px solid ${report.suspectedPairsCount > 0 ? 'rgba(239, 68, 68, 0.25)' : 'rgba(34, 197, 94, 0.25)'}`,
                    borderRadius: '8px',
                    padding: '0.85rem'
                  }}
                >
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.2rem' }}>Cặp nghi vấn (≥ {report.threshold * 100}%)</div>
                  <div style={{ fontSize: '1.25rem', fontWeight: 800, color: report.suspectedPairsCount > 0 ? '#ef4444' : '#10b981' }}>
                    {report.suspectedPairsCount}
                  </div>
                </div>
                <div style={{ background: 'rgba(255, 255, 255, 0.03)', border: '1px solid var(--panel-border)', borderRadius: '8px', padding: '0.85rem' }}>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.2rem' }}>Thời gian quét</div>
                  <div style={{ fontSize: '0.85rem', fontWeight: 600 }}>{new Date(report.checkedAt).toLocaleString()}</div>
                </div>
              </div>

              {/* Suspected Pairs List */}
              {report.suspectedPairs.length === 0 ? (
                <div
                  style={{
                    textAlign: 'center',
                    padding: '2.5rem',
                    background: 'rgba(34, 197, 94, 0.05)',
                    border: '1px solid rgba(34, 197, 94, 0.2)',
                    borderRadius: '8px',
                    color: '#10b981'
                  }}
                >
                  <CheckCircle size={36} style={{ margin: '0 auto 0.75rem', display: 'block' }} />
                  <p style={{ fontWeight: 700, fontSize: '1rem', margin: 0 }}>An toàn - Không phát hiện gian lận!</p>
                  <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginTop: '0.35rem' }}>
                    Không có cặp bài nộp nào vượt quá ngưỡng tương đồng {report.threshold * 100}%.
                  </p>
                </div>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                  <h4 style={{ fontSize: '0.95rem', fontWeight: 700, margin: '0.5rem 0 0.25rem', color: 'var(--text-main)' }}>
                    Danh Sách Cặp Bài Có Độ Trùng Lặp Cao:
                  </h4>

                  {report.suspectedPairs.map((pair, idx) => {
                    const isHigh = pair.similarityPercentage >= 85;
                    const isSelected = selectedPair === pair;

                    return (
                      <div
                        key={idx}
                        style={{
                          background: isSelected ? 'rgba(242, 113, 33, 0.08)' : 'rgba(255, 255, 255, 0.02)',
                          border: `1px solid ${isSelected ? 'var(--color-primary)' : 'var(--panel-border)'}`,
                          borderRadius: '8px',
                          padding: '0.85rem 1rem',
                          transition: 'all 0.2s ease'
                        }}
                      >
                        <div
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'space-between',
                            flexWrap: 'wrap',
                            gap: '0.75rem'
                          }}
                        >
                          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                            <div>
                              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block' }}>Sinh viên 1</span>
                              <b style={{ color: 'var(--text-main)', fontSize: '0.9rem' }}>{pair.student1Name}</b>
                              <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginLeft: '0.35rem' }}>({pair.student1Id})</span>
                            </div>

                            <span style={{ color: 'var(--text-muted)', fontWeight: 700 }}>⚡</span>

                            <div>
                              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block' }}>Sinh viên 2</span>
                              <b style={{ color: 'var(--text-main)', fontSize: '0.9rem' }}>{pair.student2Name}</b>
                              <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginLeft: '0.35rem' }}>({pair.student2Id})</span>
                            </div>
                          </div>

                          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                            <div style={{ textAlign: 'right' }}>
                              <span
                                style={{
                                  fontSize: '1.05rem',
                                  fontWeight: 800,
                                  color: isHigh ? '#ef4444' : '#f97316'
                                }}
                              >
                                {pair.similarityPercentage.toFixed(1)}% Trùng lặp
                              </span>
                              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block' }}>
                                {pair.sharedFingerprintsCount} fingerprints trùng
                              </span>
                            </div>

                            <button
                              type="button"
                              onClick={() => setSelectedPair(isSelected ? null : pair)}
                              className="btn btn-secondary btn-sm"
                              style={{ display: 'flex', alignItems: 'center', gap: '0.3rem', fontSize: '0.8rem' }}
                            >
                              <Code size={14} />
                              {isSelected ? 'Thu gọn' : 'So khớp Code'}
                            </button>
                          </div>
                        </div>

                        {/* Matched Snippets Preview */}
                        {isSelected && (
                          <div style={{ marginTop: '1rem', borderTop: '1px solid var(--panel-border)', paddingTop: '1rem' }}>
                            <h5 style={{ fontSize: '0.85rem', fontWeight: 700, marginBottom: '0.5rem', color: 'var(--color-primary)' }}>
                              Các đoạn mã nguồn tương đồng được phát hiện:
                            </h5>

                            {pair.matchedSnippets && pair.matchedSnippets.length > 0 ? (
                              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                                {pair.matchedSnippets.map((snippet, sIdx) => (
                                  <div
                                    key={sIdx}
                                    style={{
                                      display: 'grid',
                                      gridTemplateColumns: '1fr 1fr',
                                      gap: '0.75rem',
                                      background: 'rgba(0, 0, 0, 0.3)',
                                      padding: '0.75rem',
                                      borderRadius: '6px'
                                    }}
                                  >
                                    <div>
                                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.25rem' }}>
                                        Mã nguồn ({pair.student1Id}):
                                      </div>
                                      <pre
                                        style={{
                                          margin: 0,
                                          fontSize: '0.8rem',
                                          background: 'rgba(0,0,0,0.4)',
                                          padding: '0.5rem',
                                          borderRadius: '4px',
                                          overflowX: 'auto',
                                          color: '#f87171'
                                        }}
                                      >
                                        <code>{snippet.snippet1}</code>
                                      </pre>
                                    </div>

                                    <div>
                                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.25rem' }}>
                                        Mã nguồn ({pair.student2Id}):
                                      </div>
                                      <pre
                                        style={{
                                          margin: 0,
                                          fontSize: '0.8rem',
                                          background: 'rgba(0,0,0,0.4)',
                                          padding: '0.5rem',
                                          borderRadius: '4px',
                                          overflowX: 'auto',
                                          color: '#f87171'
                                        }}
                                      >
                                        <code>{snippet.snippet2}</code>
                                      </pre>
                                    </div>
                                  </div>
                                ))}
                              </div>
                            ) : (
                              <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                                Tương đồng cấu trúc token Winnowing trong toàn bộ file nộp bài.
                              </p>
                            )}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}
            </>
          )}
        </div>

        {/* Modal Footer */}
        <div
          style={{
            padding: '0.75rem 1.5rem',
            borderTop: '1px solid var(--panel-border)',
            background: 'rgba(255, 255, 255, 0.02)',
            display: 'flex',
            justifyContent: 'flex-end'
          }}
        >
          <button type="button" onClick={onClose} className="btn btn-secondary btn-sm">
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}
