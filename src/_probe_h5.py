import h5py
import numpy as np

# 1) feature_slice.h5 : inspect one feature_slices group in detail
p = r'C:\Users\Administrator\Downloads\Visium_HD_6p5mm_Rat_Liver_feature_slice.h5'
f = h5py.File(p, 'r')
print('### feature_slice.h5 top-level groups:', list(f.keys()))
fs = f['feature_slices']
keys = list(fs.keys())
print('### feature_slices count =', len(keys), ' first few =', keys[:5])
g = fs[keys[0]]
print('### group', keys[0], 'attrs:', dict(g.attrs))
for ds in ['row', 'col', 'data']:
    a = g[ds][:]
    print(f'  {ds}: min={a.min()} max={a.max()} nnz={a.size} sample={a[:5]}')
# total nnz across all slices (quick head check on a few)
nnz_total = 0
for k in keys[:50]:
    nnz_total += fs[k]['data'].shape[0]
print('### partial nnz of first 50 slices =', nnz_total, 'of', len(keys), 'slices')
f.close()

print()
# 2) molecule_info.h5 : relationships
p2 = r'C:\Users\Administrator\Downloads\Visium_HD_6p5mm_Rat_Liver_molecule_info.h5'
f2 = h5py.File(p2, 'r')
print('### molecule_info.h5 top-level keys:', list(f2.keys()))
for ds in ['count', 'feature_idx', 'gem_group', 'library_idx', 'umi', 'umi_type', 'probe_idx']:
    a = f2[ds]
    print(f'  {ds}: shape={a.shape} dtype={a.dtype}')
    if a.shape[0] > 0:
        print(f'       first5={a[:5]}')
bc = f2['barcodes']
print('  barcodes shape=', bc.shape, 'sample=', [x.decode() for x in bc[:3]])
fi = f2['features']
print('  features keys=', list(fi.keys()))
fi_g = f2['features/genome']
print('  features/genome sample=', [x.decode() for x in fi_g[:3]], 'n=', fi_g.shape[0])
# unique library_idx / gem_group
print('  library_idx uniq=', np.unique(f2['library_idx'][:1000000]))
print('  gem_group uniq=', np.unique(f2['gem_group'][:1000000]))
f2.close()
