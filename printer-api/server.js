const express = require('express');
const cors = require('cors');

const app = express();
app.use(cors());
app.use(express.json());

app.post('/print', (req, res) => {
  const { invoice } = req.body;
  
  console.log(`\n=== PRINTING RECEIPT ===`);
  console.log(`Invoice ID: ${invoice.id}`);
  console.log(`Total: $${invoice.totalAmount}`);
  console.log(`Discount: $${invoice.discount}`);
  console.log(`========================\n`);

  res.json({ success: true, message: "Printed successfully" });
});

const PORT = 3001;
app.listen(PORT, () => {
  console.log(`Silent Printing Backend running on http://localhost:${PORT}`);
});
