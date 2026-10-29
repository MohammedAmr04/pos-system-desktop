UPDATE BranchRuntimeConfig SET centralToken = NULL, updatedAt = CURRENT_TIMESTAMP WHERE centralToken IS NOT NULL;
